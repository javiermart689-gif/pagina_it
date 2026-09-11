using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

[Authorize]
public class EmailAccountsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<EmailAccountsController> _logger;

    public EmailAccountsController(ApplicationDbContext context, IEncryptionService encryptionService,
        IAuditService auditService, ILogger<EmailAccountsController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var query = _context.EmailAccounts.Include(e => e.Employee).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e => EF.Functions.Like(e.Email, $"%{term}%") || EF.Functions.Like(e.Username, $"%{term}%"));
        }

        ViewBag.SearchTerm = searchTerm;
        return View(await query.OrderBy(e => e.Email).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var account = await _context.EmailAccounts.Include(e => e.Employee).FirstOrDefaultAsync(e => e.Id == id);
        if (account is null) return NotFound();

        var vm = new EmailAccountDetailViewModel
        {
            EmailAccount = account,
            Credentials = await _context.Credentials.Where(c => c.EmailAccountId == id && c.IsActive).ToListAsync()
        };

        return View(vm);
    }

    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Create(int? employeeId)
    {
        return View(await BuildFormAsync(new EmailAccountFormViewModel { Status = EmailAccountStatus.Activa, EmployeeId = employeeId }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Create(EmailAccountFormViewModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Password))
        {
            ModelState.AddModelError(nameof(input.Password), "La contraseña es obligatoria.");
        }

        if (await _context.EmailAccounts.AnyAsync(e => e.Email == input.Email))
        {
            ModelState.AddModelError(nameof(input.Email), "Ya existe una cuenta con este correo.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        var account = new EmailAccount
        {
            EmployeeId = input.EmployeeId,
            Email = input.Email,
            Username = input.Username,
            EncryptedPassword = _encryptionService.Encrypt(input.Password),
            AccountType = input.AccountType,
            License = input.License,
            Status = input.Status,
            Notes = input.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailAccounts.Add(account);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.CrearCorreo, nameof(EmailAccount), account.Id.ToString(), account.Email);

        TempData["SuccessMessage"] = "Cuenta de correo creada correctamente.";
        return RedirectToAction(nameof(Details), new { id = account.Id });
    }

    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Edit(int id)
    {
        var account = await _context.EmailAccounts.FindAsync(id);
        if (account is null) return NotFound();

        var input = new EmailAccountFormViewModel
        {
            Id = account.Id,
            EmployeeId = account.EmployeeId,
            Email = account.Email,
            Username = account.Username,
            AccountType = account.AccountType,
            License = account.License,
            Status = account.Status,
            Notes = account.Notes
        };

        return View(await BuildFormAsync(input));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Edit(int id, EmailAccountFormViewModel input)
    {
        if (id != input.Id) return NotFound();

        if (await _context.EmailAccounts.AnyAsync(e => e.Email == input.Email && e.Id != id))
        {
            ModelState.AddModelError(nameof(input.Email), "Ya existe una cuenta con este correo.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        var account = await _context.EmailAccounts.FindAsync(id);
        if (account is null) return NotFound();

        account.EmployeeId = input.EmployeeId;
        account.Email = input.Email;
        account.Username = input.Username;
        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            account.EncryptedPassword = _encryptionService.Encrypt(input.Password);
        }
        account.AccountType = input.AccountType;
        account.License = input.License;
        account.Status = input.Status;
        account.Notes = input.Notes;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarCorreo, nameof(EmailAccount), account.Id.ToString(), account.Email);

        TempData["SuccessMessage"] = "Cuenta de correo actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id = account.Id });
    }

    /// <summary>
    /// Desencripta la contraseña únicamente en memoria y la registra en auditoría.
    /// Restringido a roles autorizados de IT (misma política que Credentials).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeVerCredenciales)]
    public async Task<IActionResult> RevealPassword(int id)
    {
        var account = await _context.EmailAccounts.FindAsync(id);
        if (account is null) return NotFound();

        string password;
        try
        {
            password = _encryptionService.Decrypt(account.EncryptedPassword);
        }
        catch (Exception)
        {
            // Nunca se expone el detalle del error (clave inválida, dato corrupto, etc.):
            // solo un 400 genérico. Tampoco se registra la excepción con datos sensibles.
            _logger.LogWarning("No se pudo desencriptar la cuenta de correo #{EmailAccountId}.", account.Id);
            return BadRequest();
        }

        await _auditService.LogAsync(AuditActions.ConsultarContrasena, nameof(EmailAccount), account.Id.ToString(), $"Consulta de contraseña: '{account.Email}'.");

        return Json(new { password });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeVerCredenciales)]
    public async Task<IActionResult> CopyPassword(int id)
    {
        var account = await _context.EmailAccounts.FindAsync(id);
        if (account is null) return NotFound();

        string password;
        try
        {
            password = _encryptionService.Decrypt(account.EncryptedPassword);
        }
        catch (Exception)
        {
            _logger.LogWarning("No se pudo desencriptar la cuenta de correo #{EmailAccountId}.", account.Id);
            return BadRequest();
        }

        await _auditService.LogAsync(AuditActions.CopiarContrasena, nameof(EmailAccount), account.Id.ToString(), $"Copia de contraseña: '{account.Email}'.");

        return Json(new { password });
    }

    private async Task<EmailAccountFormViewModel> BuildFormAsync(EmailAccountFormViewModel input)
    {
        input.Employees = await _context.Employees.Where(e => e.Status == EmployeeStatus.Activo).OrderBy(e => e.LastName).ToListAsync();
        return input;
    }
}
