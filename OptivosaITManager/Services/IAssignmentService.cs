using OptivosaITManager.Models;

namespace OptivosaITManager.Services;

public enum AssignmentAction
{
    /// <summary>El equipo no tenía asignación activa: se creó una nueva.</summary>
    Created,
    /// <summary>El equipo tenía asignación activa a OTRO empleado: se cerró y se creó una nueva.</summary>
    Reassigned,
    /// <summary>El equipo ya estaba asignado exactamente al mismo empleado: no se hizo nada.</summary>
    NoChange
}

public class AssignmentResult
{
    public AssignmentAction Action { get; set; }
    public int? PreviousEmployeeId { get; set; }
    public DeviceAssignment Assignment { get; set; } = null!;
}

/// <summary>
/// Lógica única de asignar/reasignar un equipo, compartida entre AssignmentsController (una
/// asignación a la vez, desde la UI) y el importador de Excel (muchas filas de una vez). Así la
/// regla de negocio (cerrar la asignación anterior, crear la nueva, actualizar el estado del
/// equipo, registrar auditoría) vive en un solo lugar.
/// </summary>
public interface IAssignmentService
{
    Task<AssignmentResult> AssignOrReassignAsync(Device device, int employeeId, string? performedByUserId, string? notes = null);
}
