using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

/// <summary>
/// Registro histórico de cada importación de Excel confirmada (equipos, usuarios, asignaciones
/// y accesos). No almacena los datos importados en sí (esos ya viven en sus propias tablas);
/// solo el resumen de la operación, para poder auditar cuándo y qué se importó.
/// </summary>
public class ImportHistory
{
    public int Id { get; set; }

    [Required, StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    public string? ImportedByUserId { get; set; }
    public ApplicationUser? ImportedByUser { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public int TotalRows { get; set; }

    public int DevicesCreated { get; set; }
    public int DevicesUpdated { get; set; }
    public int EmployeesCreated { get; set; }
    public int EmployeesUpdated { get; set; }
    public int AssignmentsCreated { get; set; }
    public int AssignmentsReassigned { get; set; }
    public int AccessesCreated { get; set; }
    public int AccessesUpdated { get; set; }

    public int SkippedRows { get; set; }
    public int ErrorRows { get; set; }
}
