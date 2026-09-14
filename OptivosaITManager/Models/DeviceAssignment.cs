using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public class DeviceAssignment
{
    public int Id { get; set; }

    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }

    public string? AssignedByUserId { get; set; }

    // Quién registró la devolución del equipo (acción "Devolver equipo", distinta de
    // "Reasignar": aquí no se crea una asignación nueva, solo se cierra esta).
    public string? ReturnedByUserId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
