using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Número de empleado")]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Nombre(s)")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Apellido(s)")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Puesto")]
    public string? Position { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartmentId { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(10)]
    [Display(Name = "Extensión")]
    public string? Extension { get; set; }

    [Display(Name = "Ubicación")]
    public int? LocationId { get; set; }

    [Display(Name = "Estado")]
    public EmployeeStatus Status { get; set; }

    public List<Department> Departments { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
}

public class EmployeeDetailViewModel
{
    public Employee Employee { get; set; } = null!;

    /// <summary>Equipo(s) asignados actualmente, cada uno con sus credenciales de Windows (CredentialType.Equipo).</summary>
    public List<AssignedDeviceInfo> AssignedDevices { get; set; } = new();

    /// <summary>Cuentas de correo / Microsoft 365 (incluye licencia).</summary>
    public List<EmailAccount> EmailAccounts { get; set; } = new();

    /// <summary>Credenciales de Dynamics 365, independientes de las de Microsoft 365.</summary>
    public List<Credential> Dynamics365Credentials { get; set; } = new();

    /// <summary>Otros accesos extensibles: VPN, sistemas internos, aplicaciones, servicios, etc.</summary>
    public List<Credential> OtherCredentials { get; set; } = new();
}

public class AssignedDeviceInfo
{
    public Device Device { get; set; } = null!;
    public List<Credential> WindowsCredentials { get; set; } = new();
}
