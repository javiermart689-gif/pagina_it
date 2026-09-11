using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

/// <summary>
/// Genera los reportes en Excel (.xlsx) del módulo de Reportes. Ninguno de estos reportes incluye
/// contraseñas, claves de cifrado, tokens u otra información sensible: son de control administrativo
/// y operativo (activos, personas, servicios), nunca credenciales.
/// </summary>
public interface IReportService
{
    Task<byte[]> GenerateDevicesReportAsync(ReportFilterViewModel filter);

    Task<byte[]> GenerateEmployeesReportAsync(ReportFilterViewModel filter);

    Task<byte[]> GenerateServicesReportAsync(ReportFilterViewModel filter);

    Task<byte[]> GenerateGeneralReportAsync(ReportFilterViewModel filter);
}
