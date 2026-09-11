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
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public EmployeesController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? searchTerm, int? departmentFilter, int page = 1)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Location)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.FirstName, $"%{term}%") ||
                EF.Functions.Like(e.LastName, $"%{term}%") ||
                EF.Functions.Like(e.EmployeeNumber, $"%{term}%") ||
                EF.Functions.Like(e.Email, $"%{term}%"));
        }

        if (departmentFilter.HasValue) query = query.Where(e => e.DepartmentId == departmentFilter);

        const int pageSize = 20;
        var totalCount = await query.CountAsync();
        var employees = await query.OrderBy(e => e.LastName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.DepartmentFilter = departmentFilter;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();

        return View(employees);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Location)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null) return NotFound();

        // El rol Jefe no tiene "Credenciales"/"Cuentas de correo" entre sus permisos de consulta
        // (ver Security/Roles.cs): para ese caso ni siquiera se consultan esos datos, en vez de
        // solo ocultarlos en la vista.
        var canViewCredentials = User.IsInRole(Roles.SistemasTI);

        List<AssignedDeviceInfo> assignedDevicesInfo;
        List<EmailAccount> emailAccounts = new();
        List<Credential> dynamics365Credentials = new();
        List<Credential> otherCredentials = new();

        if (canViewCredentials)
        {
            var assignedDevices = await _context.Devices
                .Include(d => d.Credentials.Where(c => c.CredentialType == CredentialType.Equipo && c.IsActive))
                .Where(d => d.Assignments.Any(a => a.EmployeeId == id && a.ReturnedAt == null))
                .ToListAsync();

            assignedDevicesInfo = assignedDevices
                .Select(d => new AssignedDeviceInfo { Device = d, WindowsCredentials = d.Credentials.ToList() })
                .ToList();
            emailAccounts = await _context.EmailAccounts.Where(ea => ea.EmployeeId == id).ToListAsync();
            dynamics365Credentials = await _context.Credentials
                .Where(c => c.EmployeeId == id && c.CredentialType == CredentialType.Dynamics365 && c.IsActive)
                .ToListAsync();
            otherCredentials = await _context.Credentials
                .Where(c => c.EmployeeId == id && c.IsActive
                    && c.CredentialType != CredentialType.Equipo
                    && c.CredentialType != CredentialType.Dynamics365)
                .ToListAsync();
        }
        else
        {
            // El equipo asignado (sin credenciales) sí es información de "Consultar equipos"
            // visible para Jefe.
            var assignedDevices = await _context.Devices
                .Where(d => d.Assignments.Any(a => a.EmployeeId == id && a.ReturnedAt == null))
                .ToListAsync();
            assignedDevicesInfo = assignedDevices
                .Select(d => new AssignedDeviceInfo { Device = d, WindowsCredentials = new List<Credential>() })
                .ToList();
        }

        var vm = new EmployeeDetailViewModel
        {
            Employee = employee,
            AssignedDevices = assignedDevicesInfo,
            EmailAccounts = emailAccounts,
            Dynamics365Credentials = dynamics365Credentials,
            OtherCredentials = otherCredentials,
            CanViewCredentials = canViewCredentials
        };

        return View(vm);
    }

    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Create()
    {
        return View(await BuildFormAsync(new EmployeeFormViewModel { Status = EmployeeStatus.Activo }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Create(EmployeeFormViewModel input)
    {
        if (await _context.Employees.AnyAsync(e => e.EmployeeNumber == input.EmployeeNumber))
        {
            ModelState.AddModelError(nameof(input.EmployeeNumber), "Ya existe un empleado con este número.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        var employee = new Employee
        {
            EmployeeNumber = input.EmployeeNumber,
            FirstName = input.FirstName,
            LastName = input.LastName,
            Position = input.Position,
            DepartmentId = input.DepartmentId,
            Email = input.Email,
            Phone = input.Phone,
            Extension = input.Extension,
            LocationId = input.LocationId,
            Status = input.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.CrearUsuario, nameof(Employee), employee.Id.ToString(), $"{employee.FirstName} {employee.LastName}");

        TempData["SuccessMessage"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Details), new { id = employee.Id });
    }

    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        var input = new EmployeeFormViewModel
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Position = employee.Position,
            DepartmentId = employee.DepartmentId,
            Email = employee.Email,
            Phone = employee.Phone,
            Extension = employee.Extension,
            LocationId = employee.LocationId,
            Status = employee.Status
        };

        return View(await BuildFormAsync(input));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel input)
    {
        if (id != input.Id) return NotFound();

        if (await _context.Employees.AnyAsync(e => e.EmployeeNumber == input.EmployeeNumber && e.Id != id))
        {
            ModelState.AddModelError(nameof(input.EmployeeNumber), "Ya existe un empleado con este número.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(input));
        }

        var employee = await _context.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        employee.EmployeeNumber = input.EmployeeNumber;
        employee.FirstName = input.FirstName;
        employee.LastName = input.LastName;
        employee.Position = input.Position;
        employee.DepartmentId = input.DepartmentId;
        employee.Email = input.Email;
        employee.Phone = input.Phone;
        employee.Extension = input.Extension;
        employee.LocationId = input.LocationId;
        employee.Status = input.Status;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarUsuario, nameof(Employee), employee.Id.ToString(), $"{employee.FirstName} {employee.LastName}");

        TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id = employee.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SistemasTI)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        employee.Status = EmployeeStatus.Inactivo;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarUsuario, nameof(Employee), employee.Id.ToString(), $"Desactivado: {employee.FirstName} {employee.LastName}");

        TempData["SuccessMessage"] = "Usuario desactivado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<EmployeeFormViewModel> BuildFormAsync(EmployeeFormViewModel input)
    {
        input.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
        input.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        return input;
    }
}
