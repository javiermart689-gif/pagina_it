using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class EmailAccountFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Empleado")]
    public int? EmployeeId { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Tipo de cuenta")]
    public EmailAccountType AccountType { get; set; }

    [StringLength(150)]
    [Display(Name = "Licencia")]
    public string? License { get; set; }

    public EmailAccountStatus Status { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<Employee> Employees { get; set; } = new();
}

public class EmailAccountDetailViewModel
{
    public EmailAccount EmailAccount { get; set; } = null!;
    public List<Credential> Credentials { get; set; } = new();
}
