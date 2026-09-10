using System.ComponentModel.DataAnnotations;
using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

// DTOs de la API REST. Nunca incluyen contraseñas ni datos cifrados.

public class DeviceDto
{
    public int Id { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? ComputerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? AssignedTo { get; set; }
}

public class DeviceCreateDto
{
    [Required, StringLength(30)]
    public string InventoryNumber { get; set; } = string.Empty;

    [Required]
    public DeviceType DeviceType { get; set; }

    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? ComputerName { get; set; }
    public string? OperatingSystem { get; set; }
    public DeviceStatus Status { get; set; }
    public int? DepartmentId { get; set; }
    public int? LocationId { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AssignDto
{
    [Required]
    public int DeviceId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public string? Notes { get; set; }
}

public class ReassignDto
{
    [Required]
    public int DeviceId { get; set; }

    [Required]
    public int NewEmployeeId { get; set; }

    public string? Notes { get; set; }
}

public class MaintenanceDto
{
    public int Id { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public string Problem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
}
