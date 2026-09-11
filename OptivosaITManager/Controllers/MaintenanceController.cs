using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

// Mantenimiento no está entre los permisos de consulta del rol Jefe (ver el menú de ejemplo en
// la especificación de roles). El historial de mantenimiento de un equipo puntual sigue siendo
// visible para Jefe porque se muestra embebido en Devices/Details, que no pasa por aquí.
[Authorize(Roles = Roles.SistemasTI)]
public class MaintenanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public MaintenanceController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(MaintenanceStatus? statusFilter)
    {
        var query = _context.MaintenanceRecords.Include(m => m.Device).AsNoTracking().AsQueryable();
        if (statusFilter.HasValue) query = query.Where(m => m.Status == statusFilter);

        var items = await query.OrderByDescending(m => m.ReportedAt)
            .Select(m => new MaintenanceListItemViewModel
            {
                Id = m.Id,
                DeviceId = m.DeviceId,
                InventoryNumber = m.Device.InventoryNumber,
                DeviceLabel = (m.Device.Brand ?? "") + " " + (m.Device.Model ?? ""),
                ReportedAt = m.ReportedAt,
                Problem = m.Problem,
                Status = m.Status
            }).ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        return View(items);
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Create(int deviceId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device is null) return NotFound();

        return View(new MaintenanceFormViewModel { DeviceId = deviceId, Device = device });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Create(MaintenanceFormViewModel input)
    {
        var device = await _context.Devices.FindAsync(input.DeviceId);
        if (device is null) return NotFound();

        if (!ModelState.IsValid)
        {
            input.Device = device;
            return View(input);
        }

        var maintenance = new Maintenance
        {
            DeviceId = device.Id,
            ReportedAt = DateTime.UtcNow,
            TechnicianId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Problem = input.Problem,
            Diagnosis = input.Diagnosis,
            Solution = input.Solution,
            Status = input.Status,
            Notes = input.Notes
        };

        _context.MaintenanceRecords.Add(maintenance);

        if (device.Status != DeviceStatus.Baja)
        {
            device.Status = DeviceStatus.Mantenimiento;
            device.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.RegistrarMantenimiento, nameof(Maintenance), maintenance.Id.ToString(), $"Equipo {device.InventoryNumber}: {maintenance.Problem}");

        TempData["SuccessMessage"] = "Mantenimiento registrado correctamente.";
        return RedirectToAction("Details", "Devices", new { id = device.Id });
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Edit(int id)
    {
        var maintenance = await _context.MaintenanceRecords
            .Include(m => m.Device).ThenInclude(d => d.Assignments)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (maintenance is null) return NotFound();

        return View(new MaintenanceFormViewModel
        {
            Id = maintenance.Id,
            DeviceId = maintenance.DeviceId,
            Device = maintenance.Device,
            Problem = maintenance.Problem,
            Diagnosis = maintenance.Diagnosis,
            Solution = maintenance.Solution,
            Status = maintenance.Status,
            Notes = maintenance.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Edit(int id, MaintenanceFormViewModel input)
    {
        if (id != input.Id) return NotFound();

        var maintenance = await _context.MaintenanceRecords
            .Include(m => m.Device).ThenInclude(d => d.Assignments)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (maintenance is null) return NotFound();

        if (!ModelState.IsValid)
        {
            input.Device = maintenance.Device;
            return View(input);
        }

        maintenance.Problem = input.Problem;
        maintenance.Diagnosis = input.Diagnosis;
        maintenance.Solution = input.Solution;
        maintenance.Status = input.Status;
        maintenance.Notes = input.Notes;

        if (input.Status is MaintenanceStatus.Reparado or MaintenanceStatus.Entregado)
        {
            maintenance.CompletedAt ??= DateTime.UtcNow;
        }

        var device = maintenance.Device;
        if (input.Status == MaintenanceStatus.Entregado && device.Status == DeviceStatus.Mantenimiento)
        {
            device.Status = device.CurrentAssignment != null ? DeviceStatus.Asignado : DeviceStatus.Disponible;
            device.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ActualizarMantenimiento, nameof(Maintenance), maintenance.Id.ToString(), $"Equipo {device.InventoryNumber}: estado {maintenance.Status}");

        TempData["SuccessMessage"] = "Mantenimiento actualizado correctamente.";
        return RedirectToAction("Details", "Devices", new { id = maintenance.DeviceId });
    }
}
