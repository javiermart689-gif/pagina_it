using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public class EmailAccount
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required, StringLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Username { get; set; } = string.Empty;

    // Contraseña cifrada (AES), mismo mecanismo que Credential.
    [Required]
    public string EncryptedPassword { get; set; } = string.Empty;

    public EmailAccountType AccountType { get; set; } = EmailAccountType.Corporativo;

    // Licencia asociada (p. ej. "Microsoft 365 Business Premium"). Opcional: no todas las
    // cuentas de correo (compartidas, distribución) tienen una licencia individual.
    [StringLength(150)]
    public string? License { get; set; }

    public EmailAccountStatus Status { get; set; } = EmailAccountStatus.Activa;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();
}
