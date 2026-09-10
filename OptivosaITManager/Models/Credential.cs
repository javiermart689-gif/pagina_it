using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public class Credential
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Username { get; set; } = string.Empty;

    // Contraseña cifrada (AES). Nunca se expone en texto plano fuera del servicio de cifrado.
    [Required]
    public string EncryptedPassword { get; set; } = string.Empty;

    public CredentialType CredentialType { get; set; }

    public int? DeviceId { get; set; }
    public Device? Device { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int? EmailAccountId { get; set; }
    public EmailAccount? EmailAccount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
