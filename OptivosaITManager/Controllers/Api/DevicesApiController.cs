using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers.Api;

[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DevicesApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceDto>>> GetAll()
    {
        var devices = await _context.Devices
            .Include(d => d.Department)
            .Include(d => d.Location)
            .Include(d => d.Assignments).ThenInclude(a => a.Employee)
            .AsNoTracking()
            .ToListAsync();

        return Ok(devices.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceDto>> GetById(int id)
    {
        var device = await _context.Devices
            .Include(d => d.Department)
            .Include(d => d.Location)
            .Include(d => d.Assignments).ThenInclude(a => a.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device is null) return NotFound();
        return Ok(ToDto(device));
    }

    [HttpPost]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<ActionResult<DeviceDto>> Create(DeviceCreateDto input)
    {
        if (await _context.Devices.AnyAsync(d => d.InventoryNumber == input.InventoryNumber))
        {
            return Conflict("Ya existe un equipo con este número de inventario.");
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
            DepartmentId = input.DepartmentId,
            LocationId = input.LocationId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.CrearEquipo, nameof(Device), device.Id.ToString(), $"Inventario {device.InventoryNumber} (API)");

        return CreatedAtAction(nameof(GetById), new { id = device.Id }, ToDto(device));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.PuedeEditar)]
    public async Task<IActionResult> Update(int id, DeviceCreateDto input)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null) return NotFound();

        if (await _context.Devices.AnyAsync(d => d.InventoryNumber == input.InventoryNumber && d.Id != id))
        {
            return Conflict("Ya existe un equipo con este número de inventario.");
        }

        device.InventoryNumber = input.InventoryNumber;
        device.DeviceType = input.DeviceType;
        device.Brand = input.Brand;
        device.Model = input.Model;
        device.SerialNumber = input.SerialNumber;
        device.ComputerName = input.ComputerName;
        device.OperatingSystem = input.OperatingSystem;
        device.Status = input.Status;
        device.DepartmentId = input.DepartmentId;
        device.LocationId = input.LocationId;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.ModificarEquipo, nameof(Device), device.Id.ToString(), $"Inventario {device.InventoryNumber} (API)");

        return NoContent();
    }

    private static DeviceDto ToDto(Device d) => new()
    {
        Id = d.Id,
        InventoryNumber = d.InventoryNumber,
        DeviceType = d.DeviceType.ToString(),
        Brand = d.Brand,
        Model = d.Model,
        SerialNumber = d.SerialNumber,
        ComputerName = d.ComputerName,
        Status = d.Status.ToString(),
        Department = d.Department?.Name,
        Location = d.Location?.Name,
        AssignedTo = d.CurrentAssignment != null ? $"{d.CurrentAssignment.Employee?.FirstName} {d.CurrentAssignment.Employee?.LastName}".Trim() : null
    };
}
