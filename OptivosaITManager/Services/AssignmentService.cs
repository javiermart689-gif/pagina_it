using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;

namespace OptivosaITManager.Services;

public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AssignmentService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<AssignmentResult> AssignOrReassignAsync(Device device, int employeeId, string? performedByUserId, string? notes = null)
    {
        if (!_context.Entry(device).Collection(d => d.Assignments).IsLoaded)
        {
            await _context.Entry(device).Collection(d => d.Assignments).LoadAsync();
        }

        var current = device.CurrentAssignment;

        // Ya está asignado exactamente a la misma persona: no hay nada que hacer (esto es lo
        // que permite que una segunda importación del mismo Excel no genere reasignaciones
        // "fantasma" contra sí mismas).
        if (current != null && current.EmployeeId == employeeId)
        {
            return new AssignmentResult { Action = AssignmentAction.NoChange, Assignment = current, PreviousEmployeeId = current.EmployeeId };
        }

        var previousEmployeeId = current?.EmployeeId;
        var isReassignment = current != null;

        // 1. Cerrar la asignación anterior (si había una).
        if (current != null)
        {
            current.ReturnedAt = DateTime.UtcNow;
        }

        // 2. Crear la nueva asignación.
        var assignment = new DeviceAssignment
        {
            DeviceId = device.Id,
            EmployeeId = employeeId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = performedByUserId,
            Notes = notes
        };
        _context.DeviceAssignments.Add(assignment);

        // 3. Actualizar el estado del equipo.
        device.Status = DeviceStatus.Asignado;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 4. Registrar la operación en auditoría.
        if (isReassignment)
        {
            await _auditService.LogAsync(AuditActions.ReasignarEquipo, nameof(Device), device.Id.ToString(),
                $"Equipo {device.InventoryNumber} reasignado de empleado #{previousEmployeeId} a #{employeeId}.");
        }
        else
        {
            await _auditService.LogAsync(AuditActions.AsignarEquipo, nameof(Device), device.Id.ToString(),
                $"Equipo {device.InventoryNumber} asignado a empleado #{employeeId}.");
        }

        return new AssignmentResult
        {
            Action = isReassignment ? AssignmentAction.Reassigned : AssignmentAction.Created,
            PreviousEmployeeId = previousEmployeeId,
            Assignment = assignment
        };
    }
}
