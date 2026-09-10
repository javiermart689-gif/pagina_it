using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers.Api;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        var employees = await _context.Employees.Include(e => e.Department).AsNoTracking().ToListAsync();
        return Ok(employees.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _context.Employees.Include(e => e.Department).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null) return NotFound();
        return Ok(ToDto(employee));
    }

    private static EmployeeDto ToDto(Employee e) => new()
    {
        Id = e.Id,
        EmployeeNumber = e.EmployeeNumber,
        FullName = e.FullName,
        Position = e.Position,
        Department = e.Department?.Name,
        Email = e.Email,
        Extension = e.Extension,
        Status = e.Status.ToString()
    };
}
