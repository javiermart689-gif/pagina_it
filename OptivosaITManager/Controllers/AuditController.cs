using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Security;

namespace OptivosaITManager.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class AuditController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context)
    {
        _context = context;
    }

    // El parámetro de filtro NO puede llamarse "action": colisiona con el valor de ruta
    // implícito de MVC {controller}/{action}/{id?} (el nombre de la acción actual, "Index"),
    // que tiene mayor prioridad que la query string en el model binding por defecto. Con
    // "action" como nombre, este filtro terminaba recibiendo siempre "Index" y la lista
    // de auditoría salía vacía aunque no se aplicara ningún filtro manualmente.
    public async Task<IActionResult> Index(string? actionFilter, string? entityName, DateTime? from, DateTime? to, int page = 1)
    {
        var query = _context.AuditLogs.Include(a => a.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionFilter)) query = query.Where(a => a.Action == actionFilter);
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        const int pageSize = 30;
        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        ViewBag.ActionFilter = actionFilter;
        ViewBag.EntityName = entityName;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
        ViewBag.Entities = await _context.AuditLogs.Select(a => a.EntityName).Distinct().OrderBy(e => e).ToListAsync();

        return View(items);
    }
}
