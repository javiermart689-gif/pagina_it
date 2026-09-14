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

    /// <summary>Equipo(s) asignados actualmente. Sus accesos propios (p. ej. Windows) se
    /// consultan desde la ficha del equipo, no aquí, para no duplicar la misma información.</summary>
    public List<AssignedDeviceInfo> AssignedDevices { get; set; } = new();

    /// <summary>Todos los accesos (correo, sistemas, VPN, etc.) relacionados con este empleado.</summary>
    public List<AccessCredential> Accesses { get; set; } = new();

    /// <summary>False para el rol Jefe: Accesses viene vacía a propósito (ni siquiera se
    /// consultó) y la vista debe mostrar un aviso, no "sin registros".</summary>
    public bool CanViewCredentials { get; set; }
}

public class AssignedDeviceInfo
{
    public Device Device { get; set; } = null!;
}
