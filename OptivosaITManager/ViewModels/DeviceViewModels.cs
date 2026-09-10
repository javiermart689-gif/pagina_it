using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class DeviceListItemViewModel
{
    public int Id { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? ComputerName { get; set; }
    public DeviceStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
}

public class DeviceListViewModel
{
    public List<DeviceListItemViewModel> Devices { get; set; } = new();

    public string? SearchTerm { get; set; }
    public DeviceType? TypeFilter { get; set; }
    public DeviceStatus? StatusFilter { get; set; }
    public int? DepartmentFilter { get; set; }

    public string SortBy { get; set; } = "InventoryNumber";
    public bool SortDescending { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public List<Department> Departments { get; set; } = new();
}

public class DeviceFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Número de inventario")]
    public string InventoryNumber { get; set; } = string.Empty;

    [Display(Name = "Tipo de equipo")]
    public DeviceType DeviceType { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(100)]
    [Display(Name = "Número de serie")]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Nombre del equipo (hostname)")]
    public string? ComputerName { get; set; }

    [StringLength(100)]
    [Display(Name = "Sistema operativo")]
    public string? OperatingSystem { get; set; }

    [Display(Name = "Estado")]
    public DeviceStatus Status { get; set; }

    [Display(Name = "Ubicación")]
    public int? LocationId { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Fecha de compra")]
    [DataType(DataType.Date)]
    public DateTime? PurchaseDate { get; set; }

    [Display(Name = "Vencimiento de garantía")]
    [DataType(DataType.Date)]
    public DateTime? WarrantyExpirationDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<Location> Locations { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
}

public class DeviceDetailViewModel
{
    public Device Device { get; set; } = null!;
    public List<DeviceAssignment> AssignmentHistory { get; set; } = new();
    public List<Credential> Credentials { get; set; } = new();
    public List<Maintenance> MaintenanceHistory { get; set; } = new();
}
