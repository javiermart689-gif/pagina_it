using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

/// <summary>
/// Módulo unificado "Accesos": un acceso representa cualquier cuenta/credencial que IT deba
/// administrar (correo corporativo, cuenta de Windows/equipo, sistema, aplicación, VPN,
/// servidor, cuenta administrativa, etc.). Reemplaza a los antiguos Credential y EmailAccount,
/// que quedaban duplicando el mismo concepto en dos módulos separados.
/// Puede relacionarse opcionalmente con un empleado, con un equipo, con ambos o con ninguno
/// (p. ej. la cuenta "sa" de un servidor SQL no pertenece a una persona ni a un equipo concreto).
/// </summary>
public class AccessCredential
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public AccessType Type { get; set; }

    [Required, StringLength(150)]
    public string Username { get; set; } = string.Empty;

    // Contraseña cifrada (AES) con una clave que nunca vive en el código fuente (ver
    // EncryptionOptions / IEncryptionService). Nunca se expone en texto plano fuera de ese
    // servicio, nunca se registra en auditoría ni en logs.
    // Nullable: un acceso puede registrarse sin contraseña conocida (p. ej. importado desde
    // Excel cuando IT decide no traer contraseñas, o una cuenta de la que solo se conoce el
    // usuario). La ficha debe mostrar "Sin contraseña registrada" en ese caso, y Mostrar/Copiar
    // deben deshabilitarse en vez de intentar desencriptar un valor inexistente.
    public string? EncryptedPassword { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int? DeviceId { get; set; }
    public Device? Device { get; set; }

    // Servicio o sistema al que pertenece el acceso (p. ej. "Microsoft 365", "ERP", "Windows").
    [StringLength(150)]
    public string? ServiceName { get; set; }

    // Licencia asociada, aplica principalmente a accesos de tipo Correo (p. ej. "Microsoft 365
    // Business Premium"). Opcional para el resto de los tipos.
    [StringLength(150)]
    public string? License { get; set; }

    public AccessStatus Status { get; set; } = AccessStatus.Activo;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
