using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public class Device
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string InventoryNumber { get; set; } = string.Empty;

    public DeviceType DeviceType { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? ComputerName { get; set; }

    [StringLength(100)]
    public string? OperatingSystem { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Disponible;

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpirationDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Identificador aleatorio y no adivinable usado únicamente para localizar el equipo al
    // escanear su código QR (ruta /q/{QrToken}). Nunca contiene información sensible; el
    // acceso a la ficha real sigue requiriendo autenticación. Estable ante reasignaciones:
    // identifica al EQUIPO, no a la persona ni a una asignación puntual.
    [StringLength(64)]
    public string? QrToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DeviceAssignment> Assignments { get; set; } = new List<DeviceAssignment>();
    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();
    public ICollection<Maintenance> MaintenanceRecords { get; set; } = new List<Maintenance>();

    public DeviceAssignment? CurrentAssignment => Assignments
        .Where(a => a.ReturnedAt == null)
        .OrderByDescending(a => a.AssignedAt)
        .FirstOrDefault();
}
