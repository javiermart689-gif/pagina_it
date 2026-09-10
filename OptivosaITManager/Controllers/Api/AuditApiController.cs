using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Security;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers.Api;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = Roles.Administrador)]
public class AuditApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAll(int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        var logs = await _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();

        return Ok(logs.Select(a => new AuditLogDto
        {
            Id = a.Id,
            Timestamp = a.Timestamp,
            UserName = a.User != null ? a.User.DisplayName : null,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            Details = a.Details
        }));
    }
}
