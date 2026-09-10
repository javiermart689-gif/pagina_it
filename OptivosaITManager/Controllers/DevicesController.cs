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
public class DevicesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IExcelImportService _importService;

    public DevicesController(ApplicationDbContext context, IAuditService auditService, IExcelImportService importService)
    {
        _context = context;
        _auditService = auditService;
        _importService = importService;
    }

    public async Task<IActionResult> Index(string? searchTerm, DeviceType? typeFilter, DeviceStatus? statusFilter,
        int? departmentFilter, string sortBy = "InventoryNumber", bool sortDescending = false, int page = 1)
    {
        var query = _context.Devices
            .Include(d => d.Department)
            .Include(d => d.Location)
            .Include(d => d.Assignments).ThenInclude(a => a.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(d =>
                EF.Functions.Like(d.InventoryNumber, $"%{term}%") ||
                (d.SerialNumber != null && EF.Functions.Like(d.SerialNumber, $"%{term}%")) ||
                (d.ComputerName != null && EF.Functions.Like(d.ComputerName, $"%{term}%")) ||
                (d.Brand != null && EF.Functions.Like(d.Brand, $"%{term}%")) ||
                (d.Model != null && EF.Functions.Like(d.Model, $"%{term}%")) ||
                d.Assignments.Any(a => a.ReturnedAt == null &&
                    (EF.Functions.Like(a.Employee.FirstName, $"%{term}%") || EF.Functions.Like(a.Employee.LastName, $"%{term}%"))));
        }

        if (typeFilter.HasValue) query = query.Where(d => d.DeviceType == typeFilter);
        if (statusFilter.HasValue) query = query.Where(d => d.Status == statusFilter);
        if (departmentFilter.HasValue) query = query.Where(d => d.DepartmentId == departmentFilter);

        query = (sortBy, sortDescending) switch
        {
            ("InventoryNumber", false) => query.OrderBy(d => d.InventoryNumber),
            ("InventoryNumber", true) => query.OrderByDescending(d => d.InventoryNumber),
            ("Status", false) => query.OrderBy(d => d.Status),
            ("Status", true) => query.OrderByDescending(d => d.Status),
            ("DeviceType", false) => query.OrderBy(d => d.DeviceType),
            ("DeviceType", true) => query.OrderByDescending(d => d.DeviceType),
            _ => query.OrderBy(d => d.InventoryNumber)
        };

        const int pageSize = 20;
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var vm = new DeviceListViewModel
        {
            SearchTerm = searchTerm,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
            DepartmentFilter = departmentFilter,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(),
            Devices = items.Select(d => new DeviceListItemViewModel
            {
                Id = d.Id,
                InventoryNumber = d.InventoryNumber,
                DeviceType = d.DeviceType,
                Brand = d.Brand,
                Model = d.Model,
                ComputerName = d.ComputerName,
                Status = d.Status,
                AssignedTo = d.CurrentAssignment != null ? $"{d.CurrentAssignment.Employee.FirstName} {d.CurrentAssignment.Employee.LastName}" : null,
                Department = d.Department?.Name,
                Location = d.Location?.Name
            }).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var device = await _context.Devices
            .Include(d => d.Department)
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device is null) return NotFound();

        var vm = new DeviceDetailViewModel
        {
            Device = device,
            AssignmentHistory = await _context.DeviceAssignments
                .Include(a => a.Employee).ThenInclude(e => e.Department)
                .Where(a => a.DeviceId == id)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync(),
            Credentials = await _context.Credentials
                .Where(c => c.DeviceId == id && c.IsActive)
                .ToListAsync(),
            MaintenanceHistory = await _context.MaintenanceRecords
                .Where(m => m.DeviceId == id)
                .OrderByDescending(m => m.ReportedAt)
                .ToListAsync()
        };

        return View(vm);
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Create()
    {
        return View(await BuildFormViewModelAsync(new DeviceFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Create(DeviceFormViewModel input)
    {
        if (await _context.Devices.AnyAsync(d => d.InventoryNumber == input.InventoryNumber))
        {
            ModelState.AddModelError(nameof(input.InventoryNumber), "Ya existe un equipo con este número de inventario.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormViewModelAsync(input));
        }

        var device = new Device
        {
            InventoryNumber = input.InventoryNumber,
            DeviceType = input.DeviceType,
            Brand = input.Brand,
            Model = input.Model,
            SerialNumber = input.SerialNumber,
            ComputerName = input.ComputerName,
            OperatingSystem = input.OperatingSystem,
            Status = input.Status,
            LocationId = input.LocationId,
            DepartmentId = input.DepartmentId,
            PurchaseDate = input.PurchaseDate,
            WarrantyExpirationDate = input.WarrantyExpirationDate,
            Notes = input.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.CrearEquipo, nameof(Device), device.Id.ToString(), $"Inventario {device.InventoryNumber}");

        TempData["SuccessMessage"] = "Equipo creado correctamente.";
        return RedirectToAction(nameof(Details), new { id = device.Id });
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Edit(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null) return NotFound();

        var model = new DeviceFormViewModel
        {
            Id = device.Id,
            InventoryNumber = device.InventoryNumber,
            DeviceType = device.DeviceType,
            Brand = device.Brand,
            Model = device.Model,
            SerialNumber = device.SerialNumber,
            ComputerName = device.ComputerName,
            OperatingSystem = device.OperatingSystem,
            Status = device.Status,
            LocationId = device.LocationId,
            DepartmentId = device.DepartmentId,
            PurchaseDate = device.PurchaseDate,
            WarrantyExpirationDate = device.WarrantyExpirationDate,
            Notes = device.Notes
        };

        return View(await BuildFormViewModelAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Edit(int id, DeviceFormViewModel input)
    {
        if (id != input.Id) return NotFound();

        if (await _context.Devices.AnyAsync(d => d.InventoryNumber == input.InventoryNumber && d.Id != id))
        {
            ModelState.AddModelError(nameof(input.InventoryNumber), "Ya existe un equipo con este número de inventario.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormViewModelAsync(input));
        }

        var device = await _context.Devices.FindAsync(id);
        if (device is null) return NotFound();

        device.InventoryNumber = input.InventoryNumber;
        device.DeviceType = input.DeviceType;
        device.Brand = input.Brand;
        device.Model = input.Model;
        device.SerialNumber = input.SerialNumber;
        device.ComputerName = input.ComputerName;
        device.OperatingSystem = input.OperatingSystem;
        device.Status = input.Status;
        device.LocationId = input.LocationId;
        device.DepartmentId = input.DepartmentId;
        device.PurchaseDate = input.PurchaseDate;
        device.WarrantyExpirationDate = input.WarrantyExpirationDate;
        device.Notes = input.Notes;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarEquipo, nameof(Device), device.Id.ToString(), $"Inventario {device.InventoryNumber}");

        TempData["SuccessMessage"] = "Equipo actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id = device.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null) return NotFound();

        device.Status = DeviceStatus.Baja;
        device.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.DesactivarEquipo, nameof(Device), device.Id.ToString(), $"Inventario {device.InventoryNumber}");

        TempData["SuccessMessage"] = "Equipo dado de baja.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Roles.PuedeEditar)]
    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> ImportPreview(IFormFile file)
    {
        if (file is null || file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Debe seleccionar un archivo .xlsx válido.";
            return RedirectToAction(nameof(Import));
        }

        await using var stream = file.OpenReadStream();
        var preview = await _importService.PreviewDevicesAsync(stream);
        return View("ImportPreview", preview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> ImportConfirm(string importToken)
    {
        var imported = await _importService.ConfirmDevicesImportAsync(importToken);
        TempData["SuccessMessage"] = $"Se importaron {imported} equipos correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<DeviceFormViewModel> BuildFormViewModelAsync(DeviceFormViewModel model)
    {
        model.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        model.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
        return model;
    }
}
