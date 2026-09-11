using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

[Authorize(Roles = Roles.PuedeVerCredenciales)]
public class CredentialsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<CredentialsController> _logger;

    public CredentialsController(ApplicationDbContext context, IEncryptionService encryptionService,
        IAuditService auditService, ILogger<CredentialsController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CredentialType? typeFilter)
    {
        var query = _context.Credentials
            .Include(c => c.Device)
            .Include(c => c.Employee)
            .Include(c => c.EmailAccount)
            .Where(c => c.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (typeFilter.HasValue) query = query.Where(c => c.CredentialType == typeFilter);

        var credentials = await query.OrderBy(c => c.Name).ToListAsync();
        ViewBag.TypeFilter = typeFilter;
        return View(credentials);
    }

    [Authorize(Roles = Roles.Administrador)]
    public IActionResult Create(int? deviceId, int? employeeId, int? emailAccountId)
    {
        return View(new CredentialFormViewModel
        {
            DeviceId = deviceId,
            EmployeeId = employeeId,
            EmailAccountId = emailAccountId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Create(CredentialFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var credential = new Credential
        {
            Name = model.Name,
            Username = model.Username,
            EncryptedPassword = _encryptionService.Encrypt(model.Password),
            CredentialType = model.CredentialType,
            DeviceId = model.DeviceId,
            EmployeeId = model.EmployeeId,
            EmailAccountId = model.EmailAccountId,
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Credentials.Add(credential);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.CrearCredencial, nameof(Credential), credential.Id.ToString(), $"Credencial '{credential.Name}' creada.");

        TempData["SuccessMessage"] = "Credencial creada correctamente.";
        return RedirectToBackOrIndex(credential);
    }

    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Edit(int id)
    {
        var credential = await _context.Credentials.FindAsync(id);
        if (credential is null) return NotFound();

        return View(new CredentialFormViewModel
        {
            Id = credential.Id,
            Name = credential.Name,
            Username = credential.Username,
            Password = string.Empty,
            CredentialType = credential.CredentialType,
            DeviceId = credential.DeviceId,
            EmployeeId = credential.EmployeeId,
            EmailAccountId = credential.EmailAccountId,
            Notes = credential.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Edit(int id, CredentialFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var credential = await _context.Credentials.FindAsync(id);
        if (credential is null) return NotFound();

        credential.Name = model.Name;
        credential.Username = model.Username;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            credential.EncryptedPassword = _encryptionService.Encrypt(model.Password);
        }
        credential.CredentialType = model.CredentialType;
        credential.Notes = model.Notes;
        credential.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarCredencial, nameof(Credential), credential.Id.ToString(), $"Credencial '{credential.Name}' modificada.");

        TempData["SuccessMessage"] = "Credencial actualizada correctamente.";
        return RedirectToBackOrIndex(credential);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var credential = await _context.Credentials.FindAsync(id);
        if (credential is null) return NotFound();

        credential.IsActive = false;
        credential.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.EliminarCredencial, nameof(Credential), credential.Id.ToString(), $"Credencial '{credential.Name}' desactivada.");

        TempData["SuccessMessage"] = "Credencial eliminada.";
        return RedirectToBackOrIndex(credential);
    }

    /// <summary>
    /// Desencripta la contraseña únicamente en memoria y la registra en auditoría.
    /// Nunca se registra el valor de la contraseña en los logs.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevealPassword(int id)
    {
        var credential = await _context.Credentials.FindAsync(id);
        if (credential is null || !credential.IsActive) return NotFound();

        string password;
        try
        {
            password = _encryptionService.Decrypt(credential.EncryptedPassword);
        }
        catch (Exception)
        {
            // Clave de cifrado inválida, dato corrupto, etc.: nunca se expone el detalle
            // del error ni se registra información sensible (ni la contraseña ni la clave),
            // solo un 400 genérico y un aviso operativo con el id de la credencial.
            _logger.LogWarning("No se pudo desencriptar la credencial #{CredentialId}.", credential.Id);
            return BadRequest();
        }

        await _auditService.LogAsync(AuditActions.ConsultarContrasena, nameof(Credential), credential.Id.ToString(), $"Consulta de contraseña: '{credential.Name}'.");

        return Json(new { password });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyPassword(int id)
    {
        var credential = await _context.Credentials.FindAsync(id);
        if (credential is null || !credential.IsActive) return NotFound();

        string password;
        try
        {
            password = _encryptionService.Decrypt(credential.EncryptedPassword);
        }
        catch (Exception)
        {
            _logger.LogWarning("No se pudo desencriptar la credencial #{CredentialId}.", credential.Id);
            return BadRequest();
        }

        await _auditService.LogAsync(AuditActions.CopiarContrasena, nameof(Credential), credential.Id.ToString(), $"Copia de contraseña: '{credential.Name}'.");

        return Json(new { password });
    }

    private IActionResult RedirectToBackOrIndex(Credential credential)
    {
        if (credential.DeviceId.HasValue)
            return RedirectToAction("Details", "Devices", new { id = credential.DeviceId });
        if (credential.EmailAccountId.HasValue)
            return RedirectToAction("Details", "EmailAccounts", new { id = credential.EmailAccountId });
        return RedirectToAction(nameof(Index));
    }
}
