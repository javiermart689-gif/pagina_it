using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class MaintenanceFormViewModel
{
    public int Id { get; set; }

    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    [Required, StringLength(1000)]
    public string Problem { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Diagnosis { get; set; }

    [StringLength(1000)]
    public string? Solution { get; set; }

    public MaintenanceStatus Status { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class MaintenanceListItemViewModel
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string DeviceLabel { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public string Problem { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; }
}
