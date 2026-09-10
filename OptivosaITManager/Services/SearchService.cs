using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

/// <summary>
/// Buscador global: encuentra usuarios, equipos y correos por nombre, número de inventario,
/// número de serie, hostname o dirección de correo.
/// </summary>
public class SearchService : ISearchService
{
    private const int MaxResultsPerCategory = 15;
    private readonly ApplicationDbContext _context;

    public SearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SearchResultViewModel> SearchAsync(string term)
    {
        var result = new SearchResultViewModel();
        if (string.IsNullOrWhiteSpace(term))
        {
            return result;
        }

        var pattern = $"%{term.Trim()}%";

        result.Employees = await _context.Employees
            .Include(e => e.Department)
            .Where(e => EF.Functions.Like(e.FirstName, pattern)
                     || EF.Functions.Like(e.LastName, pattern)
                     || EF.Functions.Like(e.EmployeeNumber, pattern)
                     || EF.Functions.Like(e.Email, pattern))
            .Take(MaxResultsPerCategory)
            .Select(e => new EmployeeSearchHit
            {
                Id = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                Department = e.Department != null ? e.Department.Name : null,
                Position = e.Position
            })
            .ToListAsync();

        result.Devices = await _context.Devices
            .Where(d => EF.Functions.Like(d.InventoryNumber, pattern)
                     || (d.SerialNumber != null && EF.Functions.Like(d.SerialNumber, pattern))
                     || (d.ComputerName != null && EF.Functions.Like(d.ComputerName, pattern))
                     || (d.Brand != null && EF.Functions.Like(d.Brand, pattern))
                     || (d.Model != null && EF.Functions.Like(d.Model, pattern)))
            .Take(MaxResultsPerCategory)
            .Select(d => new DeviceSearchHit
            {
                Id = d.Id,
                Title = (d.Brand ?? "") + " " + (d.Model ?? ""),
                InventoryNumber = d.InventoryNumber,
                Status = d.Status.ToString()
            })
            .ToListAsync();

        result.Emails = await _context.EmailAccounts
            .Include(e => e.Employee)
            .Where(e => EF.Functions.Like(e.Email, pattern) || EF.Functions.Like(e.Username, pattern))
            .Take(MaxResultsPerCategory)
            .Select(e => new EmailSearchHit
            {
                Id = e.Id,
                Email = e.Email,
                EmployeeName = e.Employee != null ? e.Employee.FirstName + " " + e.Employee.LastName : null
            })
            .ToListAsync();

        return result;
    }
}
