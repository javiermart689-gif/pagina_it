using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class AccessListViewModel
{
    public List<AccessCredential> Accesses { get; set; } = new();

    public string? SearchTerm { get; set; }
    public AccessType? TypeFilter { get; set; }
}

public class AccessFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Tipo de acceso")]
    public AccessType Type { get; set; }

    [Required, StringLength(150)]
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Empleado")]
    public int? EmployeeId { get; set; }

    [Display(Name = "Equipo")]
    public int? DeviceId { get; set; }

    [StringLength(150)]
    [Display(Name = "Servicio")]
    public string? ServiceName { get; set; }

    [StringLength(150)]
    [Display(Name = "Licencia")]
    public string? License { get; set; }

    public AccessStatus Status { get; set; } = AccessStatus.Activo;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<Employee> Employees { get; set; } = new();
    public List<Device> Devices { get; set; } = new();
}
