using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

/// <summary>
/// Módulo de Reportes: control administrativo y operativo de activos, personas y servicios,
/// descargable en Excel. Disponible tanto para Sistemas / TI como para Jefe (ambos roles pueden
/// generar y descargar reportes; Jefe solo consulta, nunca administra). Ningún reporte incluye
/// contraseñas ni credenciales.
/// </summary>
[Authorize]
public class ReportsController : Controller
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ApplicationDbContext _context;
    private readonly IReportService _reportService;
    private readonly IAuditService _auditService;

    public ReportsController(ApplicationDbContext context, IReportService reportService, IAuditService auditService)
    {
        _context = context;
        _reportService = reportService;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildViewModelAsync(new ReportsIndexViewModel()));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDevices(ReportFilterViewModel filter)
    {
        var bytes = await _reportService.GenerateDevicesReportAsync(filter);
        await _auditService.LogAsync(AuditActions.GenerarReporte, "Reporte", null, "Reporte de equipos/activos generado.");
        return File(bytes, XlsxContentType, $"Reporte_Equipos_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEmployees(ReportFilterViewModel filter)
    {
        var bytes = await _reportService.GenerateEmployeesReportAsync(filter);
        await _auditService.LogAsync(AuditActions.GenerarReporte, "Reporte", null, "Reporte de personas generado.");
        return File(bytes, XlsxContentType, $"Reporte_Personas_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadServices(ReportFilterViewModel filter)
    {
        var bytes = await _reportService.GenerateServicesReportAsync(filter);
        await _auditService.LogAsync(AuditActions.GenerarReporte, "Reporte", null, "Reporte de servicios de telecomunicaciones generado.");
        return File(bytes, XlsxContentType, $"Reporte_Servicios_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadGeneral(ReportFilterViewModel filter)
    {
        var bytes = await _reportService.GenerateGeneralReportAsync(filter);
        await _auditService.LogAsync(AuditActions.GenerarReporte, "Reporte", null, "Reporte general de activos generado.");
        return File(bytes, XlsxContentType, $"Reporte_Activos_TI_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }

    private async Task<ReportsIndexViewModel> BuildViewModelAsync(ReportsIndexViewModel model)
    {
        model.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        model.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
        return model;
    }
}
