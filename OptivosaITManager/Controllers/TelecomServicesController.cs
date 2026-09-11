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
/// Catálogo/repositorio de referencia de servicios de telecomunicaciones (Internet, telefonía,
/// enlaces, datos móviles, etc.) contratados con proveedores externos. Su único objetivo es que
/// el personal de TI encuentre en segundos el número de servicio/cuenta al llamar a un proveedor.
/// NO es un sistema de tickets/incidencias: no tiene folios, prioridades, SLA ni flujo de
/// atención; ese sistema ya existe de forma independiente y este módulo no lo reemplaza.
/// </summary>
[Authorize]
public class TelecomServicesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public TelecomServicesController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? searchTerm, TelecomServiceType? typeFilter,
        int? locationFilter, string? providerFilter, bool? statusFilter = true)
    {
        var query = _context.TelecomServices
            .Include(s => s.Location)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var pattern = $"%{term}%";
            // El enum no se puede comparar con ToString() dentro de la consulta (EF Core no
            // puede traducirlo a SQL); en su lugar se resuelve aquí, en memoria, a qué tipo
            // corresponde el término (si corresponde a alguno) y se compara por igualdad.
            var typeMatch = Enum.GetValues<TelecomServiceType>()
                .Cast<TelecomServiceType?>()
                .FirstOrDefault(t => t!.Value.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));

            query = query.Where(s =>
                EF.Functions.Like(s.Provider, pattern) ||
                (s.Name != null && EF.Functions.Like(s.Name, pattern)) ||
                (s.Description != null && EF.Functions.Like(s.Description, pattern)) ||
                (s.ServiceNumber != null && EF.Functions.Like(s.ServiceNumber, pattern)) ||
                (s.PhoneNumber != null && EF.Functions.Like(s.PhoneNumber, pattern)) ||
                (s.AccountNumber != null && EF.Functions.Like(s.AccountNumber, pattern)) ||
                (s.Address != null && EF.Functions.Like(s.Address, pattern)) ||
                (s.ProviderContact != null && EF.Functions.Like(s.ProviderContact, pattern)) ||
                (s.SupportPhone != null && EF.Functions.Like(s.SupportPhone, pattern)) ||
                (s.SupportEmail != null && EF.Functions.Like(s.SupportEmail, pattern)) ||
                (s.Location != null && EF.Functions.Like(s.Location.Name, pattern)) ||
                (typeMatch.HasValue && s.ServiceType == typeMatch.Value));
        }

        if (typeFilter.HasValue) query = query.Where(s => s.ServiceType == typeFilter);
        if (locationFilter.HasValue) query = query.Where(s => s.LocationId == locationFilter);
        if (!string.IsNullOrWhiteSpace(providerFilter)) query = query.Where(s => s.Provider == providerFilter);
        if (statusFilter.HasValue) query = query.Where(s => s.IsActive == statusFilter);

        var items = await query
            .OrderBy(s => s.ServiceType).ThenBy(s => s.Provider)
            .Select(s => new TelecomServiceListItemViewModel
            {
                Id = s.Id,
                ServiceType = s.ServiceType,
                Name = s.Name,
                Provider = s.Provider,
                ServiceNumber = s.ServiceNumber,
                PhoneNumber = s.PhoneNumber,
                Location = s.Location != null ? s.Location.Name : null,
                IsActive = s.IsActive
            })
            .ToListAsync();

        var vm = new TelecomServiceListViewModel
        {
            Services = items,
            TotalCount = items.Count,
            SearchTerm = searchTerm,
            TypeFilter = typeFilter,
            LocationFilter = locationFilter,
            ProviderFilter = providerFilter,
            StatusFilter = statusFilter,
            Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(),
            Providers = await _context.TelecomServices
                .Select(s => s.Provider)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync()
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var service = await _context.TelecomServices.Include(s => s.Location).FirstOrDefaultAsync(s => s.Id == id);
        if (service is null) return NotFound();

        return View(new TelecomServiceDetailViewModel { Service = service });
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Create()
    {
        return View(await BuildFormAsync(new TelecomServiceFormViewModel { IsActive = true }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Create(TelecomServiceFormViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        if (!input.ConfirmDuplicate)
        {
            var duplicateWarning = await FindDuplicateWarningAsync(input, existingId: null);
            if (duplicateWarning is not null)
            {
                input.DuplicateWarning = duplicateWarning;
                return View(await BuildFormAsync(input));
            }
        }

        var service = new TelecomService
        {
            ServiceType = input.ServiceType,
            Name = input.Name,
            Description = input.Description,
            Provider = input.Provider,
            ServiceNumber = input.ServiceNumber,
            PhoneNumber = input.PhoneNumber,
            AccountNumber = input.AccountNumber,
            LocationId = input.LocationId,
            Address = input.Address,
            ProviderContact = input.ProviderContact,
            SupportPhone = input.SupportPhone,
            SupportEmail = input.SupportEmail,
            Notes = input.Notes,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.TelecomServices.Add(service);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.CrearServicioTelecom, nameof(TelecomService), service.Id.ToString(),
            $"{service.ServiceType} - {service.Provider}");

        TempData["SuccessMessage"] = "Servicio registrado correctamente.";
        return RedirectToAction(nameof(Details), new { id = service.Id });
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Edit(int id)
    {
        var service = await _context.TelecomServices.FindAsync(id);
        if (service is null) return NotFound();

        var input = new TelecomServiceFormViewModel
        {
            Id = service.Id,
            ServiceType = service.ServiceType,
            Name = service.Name,
            Description = service.Description,
            Provider = service.Provider,
            ServiceNumber = service.ServiceNumber,
            PhoneNumber = service.PhoneNumber,
            AccountNumber = service.AccountNumber,
            LocationId = service.LocationId,
            Address = service.Address,
            ProviderContact = service.ProviderContact,
            SupportPhone = service.SupportPhone,
            SupportEmail = service.SupportEmail,
            Notes = service.Notes,
            IsActive = service.IsActive
        };

        return View(await BuildFormAsync(input));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Edit(int id, TelecomServiceFormViewModel input)
    {
        if (id != input.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        if (!input.ConfirmDuplicate)
        {
            var duplicateWarning = await FindDuplicateWarningAsync(input, existingId: id);
            if (duplicateWarning is not null)
            {
                input.DuplicateWarning = duplicateWarning;
                return View(await BuildFormAsync(input));
            }
        }

        var service = await _context.TelecomServices.FindAsync(id);
        if (service is null) return NotFound();

        service.ServiceType = input.ServiceType;
        service.Name = input.Name;
        service.Description = input.Description;
        service.Provider = input.Provider;
        service.ServiceNumber = input.ServiceNumber;
        service.PhoneNumber = input.PhoneNumber;
        service.AccountNumber = input.AccountNumber;
        service.LocationId = input.LocationId;
        service.Address = input.Address;
        service.ProviderContact = input.ProviderContact;
        service.SupportPhone = input.SupportPhone;
        service.SupportEmail = input.SupportEmail;
        service.Notes = input.Notes;
        service.IsActive = input.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarServicioTelecom, nameof(TelecomService), service.Id.ToString(),
            $"{service.ServiceType} - {service.Provider}");

        TempData["SuccessMessage"] = "Servicio actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id = service.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var service = await _context.TelecomServices.FindAsync(id);
        if (service is null) return NotFound();

        service.IsActive = false;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.DesactivarServicioTelecom, nameof(TelecomService), service.Id.ToString(),
            $"{service.ServiceType} - {service.Provider}");

        TempData["SuccessMessage"] = "Servicio desactivado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Activate(int id)
    {
        var service = await _context.TelecomServices.FindAsync(id);
        if (service is null) return NotFound();

        service.IsActive = true;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ActivarServicioTelecom, nameof(TelecomService), service.Id.ToString(),
            $"{service.ServiceType} - {service.Provider}");

        TempData["SuccessMessage"] = "Servicio reactivado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Advierte (sin bloquear) cuando ya existe un servicio con el mismo proveedor, número de
    /// servicio y sucursal: puede ser un duplicado accidental, pero también un caso legítimo
    /// (p. ej. el mismo proveedor con dos contratos distintos), así que solo se avisa una vez.
    /// </summary>
    private async Task<string?> FindDuplicateWarningAsync(TelecomServiceFormViewModel input, int? existingId)
    {
        if (string.IsNullOrWhiteSpace(input.ServiceNumber)) return null;

        var match = await _context.TelecomServices
            .Include(s => s.Location)
            .Where(s => s.Id != existingId
                && s.Provider == input.Provider
                && s.ServiceNumber == input.ServiceNumber
                && s.LocationId == input.LocationId)
            .FirstOrDefaultAsync();

        if (match is null) return null;

        return $"Ya existe un servicio de {match.ServiceType} con el proveedor \"{match.Provider}\", " +
               $"número de servicio \"{match.ServiceNumber}\"" +
               (match.Location is not null ? $" en \"{match.Location.Name}\"" : "") +
               ". Si es un registro distinto, puede continuar y guardarlo de todas formas.";
    }

    private async Task<TelecomServiceFormViewModel> BuildFormAsync(TelecomServiceFormViewModel model)
    {
        model.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        return model;
    }
}
