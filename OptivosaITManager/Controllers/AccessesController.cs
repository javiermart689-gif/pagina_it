using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

/// <summary>
/// Módulo unificado "Accesos": administra cualquier cuenta/credencial que IT deba controlar
/// (correo corporativo, cuenta de Windows/equipo, sistema, aplicación, VPN, servidor, cuenta
/// administrativa, etc.), en reemplazo de los antiguos módulos separados Credenciales y
/// Cuentas de correo. Contiene información sensible (contraseñas cifradas), por lo que todo
/// el controlador se restringe a Sistemas / TI.
/// </summary>
[Authorize(Roles = Roles.PuedeVerCredenciales)]
public class AccessesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccessesController> _logger;

    public AccessesController(ApplicationDbContext context, IEncryptionService encryptionService,
        IAuditService auditService, ILogger<AccessesController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(AccessType? typeFilter, string? searchTerm)
    {
        var query = _context.Accesses
            .Include(a => a.Employee)
            .Include(a => a.Device)
            .Where(a => a.Status != AccessStatus.Baja)
            .AsNoTracking()
            .AsQueryable();

        if (typeFilter.HasValue) query = query.Where(a => a.Type == typeFilter);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(a =>
                EF.Functions.Like(a.Name, pattern) ||
                EF.Functions.Like(a.Username, pattern) ||
                (a.ServiceName != null && EF.Functions.Like(a.ServiceName, pattern)) ||
                (a.Employee != null && (EF.Functions.Like(a.Employee.FirstName, pattern) || EF.Functions.Like(a.Employee.LastName, pattern))) ||
                (a.Device != null && EF.Functions.Like(a.Device.InventoryNumber, pattern)));
        }

        var accesses = await query.OrderBy(a => a.Name).ToListAsync();

        return View(new AccessListViewModel { Accesses = accesses, TypeFilter = typeFilter, SearchTerm = searchTerm });
    }

    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Create(int? deviceId, int? employeeId)
    {
        return View(await BuildFormAsync(new AccessFormViewModel { DeviceId = deviceId, EmployeeId = employeeId }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Create(AccessFormViewModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Password))
        {
            ModelState.AddModelError(nameof(input.Password), "La contraseña es obligatoria.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        var access = new AccessCredential
        {
            Name = input.Name,
            Type = input.Type,
            Username = input.Username,
            EncryptedPassword = _encryptionService.Encrypt(input.Password),
            EmployeeId = input.EmployeeId,
            DeviceId = input.DeviceId,
            ServiceName = input.ServiceName,
            License = input.License,
            Status = input.Status,
            Notes = input.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accesses.Add(access);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.CrearAcceso, nameof(AccessCredential), access.Id.ToString(), $"Acceso '{access.Name}' creado.");

        TempData["SuccessMessage"] = "Acceso creado correctamente.";
        return RedirectToBackOrIndex(access);
    }

    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Edit(int id)
    {
        var access = await _context.Accesses.FindAsync(id);
        if (access is null) return NotFound();

        var input = new AccessFormViewModel
        {
            Id = access.Id,
            Name = access.Name,
            Type = access.Type,
            Username = access.Username,
            EmployeeId = access.EmployeeId,
            DeviceId = access.DeviceId,
            ServiceName = access.ServiceName,
            License = access.License,
            Status = access.Status,
            Notes = access.Notes
        };

        return View(await BuildFormAsync(input));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Edit(int id, AccessFormViewModel input)
    {
        if (id != input.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        var access = await _context.Accesses.FindAsync(id);
        if (access is null) return NotFound();

        access.Name = input.Name;
        access.Type = input.Type;
        access.Username = input.Username;
        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            access.EncryptedPassword = _encryptionService.Encrypt(input.Password);
        }
        access.EmployeeId = input.EmployeeId;
        access.DeviceId = input.DeviceId;
        access.ServiceName = input.ServiceName;
        access.License = input.License;
        access.Status = input.Status;
        access.Notes = input.Notes;
        access.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarAcceso, nameof(AccessCredential), access.Id.ToString(), $"Acceso '{access.Name}' modificado.");

        TempData["SuccessMessage"] = "Acceso actualizado correctamente.";
        return RedirectToBackOrIndex(access);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var access = await _context.Accesses.FindAsync(id);
        if (access is null) return NotFound();

        access.Status = AccessStatus.Baja;
        access.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.DesactivarAcceso, nameof(AccessCredential), access.Id.ToString(), $"Acceso '{access.Name}' desactivado.");

        TempData["SuccessMessage"] = "Acceso eliminado.";
        return RedirectToBackOrIndex(access);
    }

    /// <summary>
    /// Desencripta la contraseña únicamente en memoria y la registra en auditoría.
    /// Nunca se registra el valor de la contraseña en los logs.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevealPassword(int id)
    {
        var access = await _context.Accesses.FindAsync(id);
        if (access is null || access.Status == AccessStatus.Baja) return NotFound();

        string password;
        try
        {
            password = _encryptionService.Decrypt(access.EncryptedPassword);
        }
        catch (Exception)
        {
            // Clave de cifrado inválida, dato corrupto, etc.: nunca se expone el detalle
            // del error ni se registra información sensible (ni la contraseña ni la clave),
            // solo un 400 genérico y un aviso operativo con el id del acceso.
            _logger.LogWarning("No se pudo desencriptar el acceso #{AccessId}.", access.Id);
            return BadRequest();
        }

        await _auditService.LogAsync(AuditActions.ConsultarContrasena, nameof(AccessCredential), access.Id.ToString(), $"Consulta de contraseña: '{access.Name}'.");

        return Json(new { password });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyPassword(int id)
    {
        var access = await _context.Accesses.FindAsync(id);
        if (access is null || access.Status == AccessStatus.Baja) return NotFound();

        string password;
        try
        {
            password = _encryptionService.Decrypt(access.EncryptedPassword);
        }
        catch (Exception)
        {
            _logger.LogWarning("No se pudo desencriptar el acceso #{AccessId}.", access.Id);
            return BadRequest();
        }

        await _auditService.LogAsync(AuditActions.CopiarContrasena, nameof(AccessCredential), access.Id.ToString(), $"Copia de contraseña: '{access.Name}'.");

        return Json(new { password });
    }

    private async Task<AccessFormViewModel> BuildFormAsync(AccessFormViewModel input)
    {
        input.Employees = await _context.Employees.Where(e => e.Status == EmployeeStatus.Activo).OrderBy(e => e.LastName).ToListAsync();
        input.Devices = await _context.Devices.Where(d => d.Status != DeviceStatus.Baja).OrderBy(d => d.InventoryNumber).ToListAsync();
        return input;
    }

    private IActionResult RedirectToBackOrIndex(AccessCredential access)
    {
        if (access.DeviceId.HasValue)
            return RedirectToAction("Details", "Devices", new { id = access.DeviceId });
        if (access.EmployeeId.HasValue)
            return RedirectToAction("Details", "Employees", new { id = access.EmployeeId });
        return RedirectToAction(nameof(Index));
    }
}
