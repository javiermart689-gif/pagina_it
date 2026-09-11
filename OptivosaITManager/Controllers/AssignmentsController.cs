using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

// Asignaciones no está entre los permisos de consulta del rol Jefe (ver el menú de ejemplo en
// la especificación de roles: Jefe solo ve Personas/Equipos/Servicios/Reportes). El historial
// de asignaciones de un equipo puntual sigue siendo visible para Jefe porque se muestra
// embebido en Devices/Details, que no pasa por este controlador.
[Authorize(Roles = Roles.SistemasTI)]
public class AssignmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AssignmentsController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.DeviceAssignments
            .Include(a => a.Device)
            .Include(a => a.Employee)
            .Where(a => a.ReturnedAt == null)
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new AssignmentListItemViewModel
            {
                DeviceId = a.DeviceId,
                InventoryNumber = a.Device.InventoryNumber,
                DeviceLabel = (a.Device.Brand ?? "") + " " + (a.Device.Model ?? ""),
                EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
                AssignedAt = a.AssignedAt
            })
            .ToListAsync();

        return View(items);
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Assign(int deviceId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device is null) return NotFound();

        if (device.CurrentAssignment != null)
        {
            TempData["ErrorMessage"] = "Este equipo ya tiene un usuario asignado. Utilice Reasignar.";
            return RedirectToAction("Details", "Devices", new { id = deviceId });
        }

        return View(new AssignViewModel
        {
            DeviceId = deviceId,
            Device = device,
            Employees = await _context.Employees.Where(e => e.Status == EmployeeStatus.Activo).OrderBy(e => e.LastName).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Assign(AssignViewModel input)
    {
        var device = await _context.Devices
            .Include(d => d.Assignments)
            .FirstOrDefaultAsync(d => d.Id == input.DeviceId);
        if (device is null) return NotFound();

        if (device.CurrentAssignment != null)
        {
            ModelState.AddModelError(string.Empty, "Este equipo ya tiene un usuario asignado.");
        }

        if (!ModelState.IsValid)
        {
            input.Device = device;
            input.Employees = await _context.Employees.Where(e => e.Status == EmployeeStatus.Activo).OrderBy(e => e.LastName).ToListAsync();
            return View(input);
        }

        var assignment = new DeviceAssignment
        {
            DeviceId = device.Id,
            EmployeeId = input.EmployeeId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Notes = input.Notes
        };

        _context.DeviceAssignments.Add(assignment);
        device.Status = DeviceStatus.Asignado;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.AsignarEquipo, nameof(Device), device.Id.ToString(),
            $"Equipo {device.InventoryNumber} asignado a empleado #{input.EmployeeId}.");

        TempData["SuccessMessage"] = "Equipo asignado correctamente.";
        return RedirectToAction("Details", "Devices", new { id = device.Id });
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Reassign(int deviceId)
    {
        var device = await _context.Devices
            .Include(d => d.Assignments).ThenInclude(a => a.Employee)
            .FirstOrDefaultAsync(d => d.Id == deviceId);
        if (device is null) return NotFound();

        var current = device.CurrentAssignment;
        if (current is null)
        {
            TempData["ErrorMessage"] = "Este equipo no tiene un usuario asignado. Utilice Asignar.";
            return RedirectToAction("Details", "Devices", new { id = deviceId });
        }

        return View(new ReassignViewModel
        {
            DeviceId = deviceId,
            Device = device,
            CurrentAssignment = current,
            Employees = await _context.Employees
                .Where(e => e.Status == EmployeeStatus.Activo && e.Id != current.EmployeeId)
                .OrderBy(e => e.LastName).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Reassign(ReassignViewModel input)
    {
        var device = await _context.Devices
            .Include(d => d.Assignments).ThenInclude(a => a.Employee)
            .FirstOrDefaultAsync(d => d.Id == input.DeviceId);
        if (device is null) return NotFound();

        var current = device.CurrentAssignment;
        if (current is null)
        {
            ModelState.AddModelError(string.Empty, "Este equipo ya no tiene una asignación activa.");
        }

        if (!ModelState.IsValid)
        {
            input.Device = device;
            input.CurrentAssignment = current;
            input.Employees = await _context.Employees
                .Where(e => e.Status == EmployeeStatus.Activo && e.Id != (current != null ? current.EmployeeId : 0))
                .OrderBy(e => e.LastName).ToListAsync();
            return View(input);
        }

        // 1. Cerrar la asignación anterior.
        current!.ReturnedAt = DateTime.UtcNow;

        // 2. Crear la nueva asignación.
        var newAssignment = new DeviceAssignment
        {
            DeviceId = device.Id,
            EmployeeId = input.NewEmployeeId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Notes = input.Notes
        };
        _context.DeviceAssignments.Add(newAssignment);

        // 3. Actualizar el estado del equipo.
        device.Status = DeviceStatus.Asignado;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 4. Registrar la operación en auditoría.
        await _auditService.LogAsync(AuditActions.ReasignarEquipo, nameof(Device), device.Id.ToString(),
            $"Equipo {device.InventoryNumber} reasignado de empleado #{current.EmployeeId} a #{input.NewEmployeeId}.");

        TempData["SuccessMessage"] = "Equipo reasignado correctamente.";
        return RedirectToAction("Details", "Devices", new { id = device.Id });
    }
}
