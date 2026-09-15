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
    private readonly IAssignmentService _assignmentService;

    public AssignmentsController(ApplicationDbContext context, IAuditService auditService, IAssignmentService assignmentService)
    {
        _context = context;
        _auditService = auditService;
        _assignmentService = assignmentService;
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

        var performedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _assignmentService.AssignOrReassignAsync(device, input.EmployeeId, performedBy, input.Notes);

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

        var performedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _assignmentService.AssignOrReassignAsync(device, input.NewEmployeeId, performedBy, input.Notes);

        TempData["SuccessMessage"] = "Equipo reasignado correctamente.";
        return RedirectToAction("Details", "Devices", new { id = device.Id });
    }

    // "Devolver equipo": el usuario que lo tenía asignado lo entrega y el equipo queda
    // Disponible para asignarse a otra persona más adelante. A diferencia de Reasignar, aquí
    // no se crea una nueva asignación — solo se cierra la actual.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Return(int deviceId)
    {
        var device = await _context.Devices
            .Include(d => d.Assignments)
            .FirstOrDefaultAsync(d => d.Id == deviceId);
        if (device is null) return NotFound();

        var current = device.CurrentAssignment;
        if (current is null)
        {
            TempData["ErrorMessage"] = "Este equipo no tiene una asignación activa que devolver.";
            return RedirectToAction("Details", "Devices", new { id = deviceId });
        }

        // 1. Cerrar la asignación activa y registrar quién hizo la devolución.
        current.ReturnedAt = DateTime.UtcNow;
        current.ReturnedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // 2. El equipo queda disponible para una futura asignación (no se crea uno nuevo).
        device.Status = DeviceStatus.Disponible;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.DevolverEquipo, nameof(Device), device.Id.ToString(),
            $"Equipo {device.InventoryNumber} devuelto por empleado #{current.EmployeeId}.");

        TempData["SuccessMessage"] = "Devolución registrada. El equipo quedó disponible.";
        return RedirectToAction("Details", "Devices", new { id = device.Id });
    }
}
