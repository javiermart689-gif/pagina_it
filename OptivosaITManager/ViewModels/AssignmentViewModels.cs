using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class AssignViewModel
{
    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    [Required, Display(Name = "Usuario")]
    public int EmployeeId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<Employee> Employees { get; set; } = new();
}

public class ReassignViewModel
{
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public DeviceAssignment? CurrentAssignment { get; set; }

    [Required, Display(Name = "Nuevo usuario")]
    public int NewEmployeeId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<Employee> Employees { get; set; } = new();
}

public class AssignmentListItemViewModel
{
    public int DeviceId { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string DeviceLabel { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}
