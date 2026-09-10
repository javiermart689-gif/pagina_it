using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers.Api;

[ApiController]
[Route("api/deviceassignments")]
[Authorize(Roles = Roles.PuedeEditar)]
public class AssignmentsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AssignmentsApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<IActionResult> Assign(AssignDto input)
    {
        var device = await _context.Devices.Include(d => d.Assignments).FirstOrDefaultAsync(d => d.Id == input.DeviceId);
        if (device is null) return NotFound("Equipo no encontrado.");
        if (!await _context.Employees.AnyAsync(e => e.Id == input.EmployeeId)) return NotFound("Empleado no encontrado.");

        if (device.CurrentAssignment != null)
        {
            return Conflict("El equipo ya tiene una asignación activa. Utilice /api/deviceassignments/reassign.");
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
        await _auditService.LogAsync(AuditActions.AsignarEquipo, nameof(Device), device.Id.ToString(), $"Asignado vía API a empleado #{input.EmployeeId}");

        return Ok(new { assignment.Id, assignment.DeviceId, assignment.EmployeeId, assignment.AssignedAt });
    }

    [HttpPost("reassign")]
    public async Task<IActionResult> Reassign(ReassignDto input)
    {
        var device = await _context.Devices.Include(d => d.Assignments).FirstOrDefaultAsync(d => d.Id == input.DeviceId);
        if (device is null) return NotFound("Equipo no encontrado.");
        if (!await _context.Employees.AnyAsync(e => e.Id == input.NewEmployeeId)) return NotFound("Empleado no encontrado.");

        var current = device.CurrentAssignment;
        if (current is null)
        {
            return Conflict("El equipo no tiene una asignación activa. Utilice /api/deviceassignments.");
        }

        current.ReturnedAt = DateTime.UtcNow;

        var newAssignment = new DeviceAssignment
        {
            DeviceId = device.Id,
            EmployeeId = input.NewEmployeeId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Notes = input.Notes
        };
        _context.DeviceAssignments.Add(newAssignment);

        device.Status = DeviceStatus.Asignado;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ReasignarEquipo, nameof(Device), device.Id.ToString(),
            $"Reasignado vía API de empleado #{current.EmployeeId} a #{input.NewEmployeeId}");

        return Ok(new { newAssignment.Id, newAssignment.DeviceId, newAssignment.EmployeeId, newAssignment.AssignedAt });
    }
}
