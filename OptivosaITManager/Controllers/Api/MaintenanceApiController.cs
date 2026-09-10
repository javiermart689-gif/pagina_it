using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers.Api;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MaintenanceApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaintenanceDto>>> GetAll(int? deviceId)
    {
        var query = _context.MaintenanceRecords.Include(m => m.Device).AsNoTracking().AsQueryable();
        if (deviceId.HasValue) query = query.Where(m => m.DeviceId == deviceId);

        var records = await query.OrderByDescending(m => m.ReportedAt).ToListAsync();

        return Ok(records.Select(m => new MaintenanceDto
        {
            Id = m.Id,
            InventoryNumber = m.Device.InventoryNumber,
            ReportedAt = m.ReportedAt,
            Problem = m.Problem,
            Status = m.Status.ToString(),
            CompletedAt = m.CompletedAt
        }));
    }
}
