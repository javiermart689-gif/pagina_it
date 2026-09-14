using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Services;

namespace OptivosaITManager.Controllers;

/// <summary>
/// Punto de entrada al escanear el QR físico de un equipo (ruta corta /q/{token}).
/// El token nunca contiene información sensible; solo localiza al equipo. El acceso real
/// a la ficha sigue exigiendo autenticación (ASP.NET Core Identity redirige a Login con
/// returnUrl si el usuario no ha iniciado sesión) — así el QR nunca expone datos por sí solo.
/// </summary>
[Authorize]
public class QrController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public QrController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet("/q/{token}")]
    public async Task<IActionResult> Scan(string token)
    {
        var device = await _context.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.QrToken == token);
        if (device is null)
        {
            TempData["ErrorMessage"] = "Este código QR no corresponde a ningún equipo registrado. Puede registrarlo como equipo nuevo.";
            return RedirectToAction("FindOrRegister", "Devices");
        }

        await _auditService.LogAsync(AuditActions.EscanearQR, nameof(Device), device.Id.ToString(),
            $"QR escaneado para el equipo {device.InventoryNumber}.");

        return RedirectToAction("Details", "Devices", new { id = device.Id });
    }
}
