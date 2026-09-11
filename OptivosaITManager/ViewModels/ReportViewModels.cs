using OptivosaITManager.Models;

namespace OptivosaITManager.ViewModels;

/// <summary>
/// Filtros comunes para los cuatro reportes. Cada reporte usa solo el subconjunto que le aplica
/// (p. ej. el reporte de servicios ignora DeviceType/DeviceStatus).
/// </summary>
public class ReportFilterViewModel
{
    public int? LocationId { get; set; }
    public int? DepartmentId { get; set; }
    public DeviceType? DeviceType { get; set; }
    public DeviceStatus? DeviceStatus { get; set; }
    public TelecomServiceType? ServiceType { get; set; }
    public bool? ServiceStatus { get; set; } = true;
}

public class ReportsIndexViewModel : ReportFilterViewModel
{
    public List<Location> Locations { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
}
