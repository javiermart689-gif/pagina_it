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

    public async Task<IActionResult> Index(string? action, string? entityName, DateTime? from, DateTime? to, int page = 1)
    {
        var query = _context.AuditLogs.Include(a => a.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        const int pageSize = 30;
        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        ViewBag.Action = action;
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
