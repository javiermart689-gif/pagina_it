using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

[Authorize(Roles = Roles.SistemasTI)]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public SettingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager, IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public IActionResult Index() => View();

    #region Departamentos

    public async Task<IActionResult> Departments()
    {
        return View(await _context.Departments.OrderBy(d => d.Name).ToListAsync());
    }

    public IActionResult DepartmentCreate() => View("CatalogForm", new CatalogItemFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentCreate(CatalogItemFormViewModel input)
    {
        if (!ModelState.IsValid) return View("CatalogForm", input);

        _context.Departments.Add(new Department { Name = input.Name, Description = input.Description, IsActive = true });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Departamento creado.";
        return RedirectToAction(nameof(Departments));
    }

    public async Task<IActionResult> DepartmentEdit(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department is null) return NotFound();
        return View("CatalogForm", new CatalogItemFormViewModel { Id = department.Id, Name = department.Name, Description = department.Description, IsActive = department.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentEdit(CatalogItemFormViewModel input)
    {
        if (!ModelState.IsValid) return View("CatalogForm", input);

        var department = await _context.Departments.FindAsync(input.Id);
        if (department is null) return NotFound();

        department.Name = input.Name;
        department.Description = input.Description;
        department.IsActive = input.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Departamento actualizado.";
        return RedirectToAction(nameof(Departments));
    }

    #endregion

    #region Ubicaciones

    public async Task<IActionResult> Locations()
    {
        return View(await _context.Locations.OrderBy(l => l.Name).ToListAsync());
    }

    public IActionResult LocationCreate() => View("CatalogForm", new CatalogItemFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocationCreate(CatalogItemFormViewModel input)
    {
        if (!ModelState.IsValid) return View("CatalogForm", input);

        _context.Locations.Add(new Location { Name = input.Name, Address = input.Description, IsActive = true });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Ubicación creada.";
        return RedirectToAction(nameof(Locations));
    }

    public async Task<IActionResult> LocationEdit(int id)
    {
        var location = await _context.Locations.FindAsync(id);
        if (location is null) return NotFound();
        return View("CatalogForm", new CatalogItemFormViewModel { Id = location.Id, Name = location.Name, Description = location.Address, IsActive = location.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocationEdit(CatalogItemFormViewModel input)
    {
        if (!ModelState.IsValid) return View("CatalogForm", input);

        var location = await _context.Locations.FindAsync(input.Id);
        if (location is null) return NotFound();

        location.Name = input.Name;
        location.Address = input.Description;
        location.IsActive = input.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ubicación actualizada.";
        return RedirectToAction(nameof(Locations));
    }

    #endregion

    #region Usuarios y roles

    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var items = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            items.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                LockedOut = await _userManager.IsLockedOutAsync(user)
            });
        }
        return View(items);
    }

    public IActionResult CreateUser() => View(new CreateUserViewModel { AllRoles = Security.Roles.All.ToList() });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel input)
    {
        if (!ModelState.IsValid)
        {
            input.AllRoles = Security.Roles.All.ToList();
            return View(input);
        }

        var user = new ApplicationUser { UserName = input.Email, Email = input.Email, DisplayName = input.DisplayName, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            input.AllRoles = Security.Roles.All.ToList();
            return View(input);
        }

        await _userManager.AddToRoleAsync(user, input.Role);
        await _auditService.LogAsync(AuditActions.CrearUsuario, nameof(ApplicationUser), user.Id, $"Usuario del sistema creado: {user.Email} ({input.Role})");

        TempData["SuccessMessage"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> EditUserRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        return View(new UserRolesViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            AllRoles = Security.Roles.All.ToList(),
            SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUserRoles(string userId, List<string> selectedRoles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, selectedRoles);

        await _auditService.LogAsync(AuditActions.ModificarUsuario, nameof(ApplicationUser), user.Id, $"Roles actualizados: {string.Join(", ", selectedRoles)}");

        TempData["SuccessMessage"] = "Roles actualizados correctamente.";
        return RedirectToAction(nameof(Users));
    }

    #endregion
}
