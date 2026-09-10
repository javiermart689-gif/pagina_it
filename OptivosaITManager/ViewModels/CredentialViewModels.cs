using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class CredentialFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Tipo de credencial")]
    public CredentialType CredentialType { get; set; }

    public int? DeviceId { get; set; }
    public int? EmployeeId { get; set; }
    public int? EmailAccountId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public string? DeviceLabel { get; set; }
    public string? EmployeeLabel { get; set; }
    public string? EmailAccountLabel { get; set; }
}
