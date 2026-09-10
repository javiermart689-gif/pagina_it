using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public class Maintenance
{
    public int Id { get; set; }

    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public string? TechnicianId { get; set; }
    public ApplicationUser? Technician { get; set; }

    [Required, StringLength(1000)]
    public string Problem { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Diagnosis { get; set; }

    [StringLength(1000)]
    public string? Solution { get; set; }

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Reportado;

    public DateTime? CompletedAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
