using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

/// <summary>
/// Catálogo de referencia de servicios de telecomunicaciones (Internet, telefonía, enlaces, etc.)
/// contratados con proveedores externos. Es únicamente información de consulta para que el
/// personal de TI localice rápidamente el número de servicio/cuenta al contactar a un proveedor.
/// NO es un sistema de tickets ni de incidencias: no tiene folios, prioridades, SLA ni flujo de
/// atención; ese sistema ya existe de forma independiente.
/// </summary>
public class TelecomService
{
    public int Id { get; set; }

    public TelecomServiceType ServiceType { get; set; }

    // Alias descriptivo para distinguir servicios del mismo tipo/sucursal (p. ej. "Internet
    // principal" vs "Internet respaldo"). No es obligatorio: el tipo + proveedor + sucursal
    // ya identifican razonablemente al servicio.
    [StringLength(150)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(150)]
    public string Provider { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ServiceNumber { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? AccountNumber { get; set; }

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(150)]
    public string? ProviderContact { get; set; }

    [StringLength(30)]
    public string? SupportPhone { get; set; }

    [StringLength(150)]
    public string? SupportEmail { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Baja lógica: nunca se elimina físicamente un registro para conservar el historial.
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
