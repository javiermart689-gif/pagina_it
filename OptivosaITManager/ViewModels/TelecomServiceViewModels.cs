using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

public class TelecomServiceListItemViewModel
{
    public int Id { get; set; }
    public TelecomServiceType ServiceType { get; set; }
    public string? Name { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ServiceNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
}

public class TelecomServiceListViewModel
{
    public List<TelecomServiceListItemViewModel> Services { get; set; } = new();

    public string? SearchTerm { get; set; }
    public TelecomServiceType? TypeFilter { get; set; }
    public int? LocationFilter { get; set; }
    public string? ProviderFilter { get; set; }
    // null = todos, true = solo activos, false = solo inactivos.
    public bool? StatusFilter { get; set; } = true;

    public List<Location> Locations { get; set; } = new();
    public List<string> Providers { get; set; } = new();

    public int TotalCount { get; set; }
}

public class TelecomServiceFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Tipo de servicio")]
    public TelecomServiceType ServiceType { get; set; }

    [StringLength(150)]
    [Display(Name = "Nombre / alias")]
    public string? Name { get; set; }

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Proveedor")]
    public string Provider { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Número de servicio")]
    public string? ServiceNumber { get; set; }

    [StringLength(30)]
    [Display(Name = "Número telefónico")]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Número de cuenta")]
    public string? AccountNumber { get; set; }

    [Display(Name = "Sucursal")]
    public int? LocationId { get; set; }

    [StringLength(300)]
    [Display(Name = "Dirección")]
    public string? Address { get; set; }

    [StringLength(150)]
    [Display(Name = "Contacto del proveedor")]
    public string? ProviderContact { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono de soporte")]
    public string? SupportPhone { get; set; }

    [StringLength(150), EmailAddress]
    [Display(Name = "Correo de soporte")]
    public string? SupportEmail { get; set; }

    [StringLength(1000)]
    [Display(Name = "Observaciones")]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // Marcado por el usuario cuando se le advirtió de un posible duplicado y decide continuar.
    public bool ConfirmDuplicate { get; set; }

    public List<Location> Locations { get; set; } = new();

    /// <summary>Mensaje no bloqueante mostrado cuando ya existe un servicio similar (mismo proveedor + número/sucursal).</summary>
    public string? DuplicateWarning { get; set; }
}

public class TelecomServiceDetailViewModel
{
    public TelecomService Service { get; set; } = null!;
}
