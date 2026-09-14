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
            .Include(e => e.Location)
            .Where(e => EF.Functions.Like(e.FirstName, pattern)
                     || EF.Functions.Like(e.LastName, pattern)
                     || EF.Functions.Like(e.EmployeeNumber, pattern)
                     || EF.Functions.Like(e.Email, pattern)
                     || (e.Phone != null && EF.Functions.Like(e.Phone, pattern))
                     || (e.Department != null && EF.Functions.Like(e.Department.Name, pattern))
                     || (e.Location != null && EF.Functions.Like(e.Location.Name, pattern))
                     || e.DeviceAssignments.Any(a => a.ReturnedAt == null && EF.Functions.Like(a.Device.InventoryNumber, pattern))
                     || e.Accesses.Any(ac => EF.Functions.Like(ac.Username, pattern) || EF.Functions.Like(ac.Name, pattern)))
            .Take(MaxResultsPerCategory)
            .Select(e => new EmployeeSearchHit
            {
                Id = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                Department = e.Department != null ? e.Department.Name : null,
                Position = e.Position,
                Location = e.Location != null ? e.Location.Name : null,
                Phone = e.Phone,
                AssignedDeviceInventoryNumber = e.DeviceAssignments
                    .Where(a => a.ReturnedAt == null)
                    .Select(a => a.Device.InventoryNumber)
                    .FirstOrDefault()
            })
            .ToListAsync();

        result.Devices = await _context.Devices
            .Where(d => EF.Functions.Like(d.InventoryNumber, pattern)
                     || (d.SerialNumber != null && EF.Functions.Like(d.SerialNumber, pattern))
                     || (d.ComputerName != null && EF.Functions.Like(d.ComputerName, pattern))
                     || (d.Brand != null && EF.Functions.Like(d.Brand, pattern))
                     || (d.Model != null && EF.Functions.Like(d.Model, pattern))
                     || (d.Department != null && EF.Functions.Like(d.Department.Name, pattern))
                     || (d.Location != null && EF.Functions.Like(d.Location.Name, pattern))
                     || d.Assignments.Any(a => a.ReturnedAt == null &&
                            (EF.Functions.Like(a.Employee.FirstName, pattern) || EF.Functions.Like(a.Employee.LastName, pattern))))
            .Take(MaxResultsPerCategory)
            .Select(d => new DeviceSearchHit
            {
                Id = d.Id,
                Title = (d.Brand ?? "") + " " + (d.Model ?? ""),
                InventoryNumber = d.InventoryNumber,
                Status = d.Status.ToString()
            })
            .ToListAsync();

        result.Accesses = await _context.Accesses
            .Include(a => a.Employee)
            .Include(a => a.Device)
            .Where(a => a.Status != Models.AccessStatus.Baja &&
                     (EF.Functions.Like(a.Name, pattern)
                     || EF.Functions.Like(a.Username, pattern)
                     || (a.ServiceName != null && EF.Functions.Like(a.ServiceName, pattern))
                     || (a.Employee != null && (EF.Functions.Like(a.Employee.FirstName, pattern) || EF.Functions.Like(a.Employee.LastName, pattern)))
                     || (a.Device != null && EF.Functions.Like(a.Device.InventoryNumber, pattern))))
            .Take(MaxResultsPerCategory)
            .Select(a => new AccessSearchHit
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type.ToString(),
                Username = a.Username,
                RelatedTo = a.Employee != null ? a.Employee.FirstName + " " + a.Employee.LastName
                    : a.Device != null ? a.Device.InventoryNumber : null
            })
            .ToListAsync();

        // El enum ServiceType no se puede comparar/proyectar con ToString() dentro de la
        // consulta (EF Core no puede traducirlo a SQL); se resuelve en memoria a qué tipo
        // corresponde el término buscado (si corresponde a alguno) antes de construir el Where.
        var telecomTypeMatch = Enum.GetValues<Models.TelecomServiceType>()
            .Cast<Models.TelecomServiceType?>()
            .FirstOrDefault(t => t!.Value.ToString().Contains(term.Trim(), StringComparison.OrdinalIgnoreCase));

        result.TelecomServices = (await _context.TelecomServices
            .Include(s => s.Location)
            .Where(s => s.IsActive &&
                     (EF.Functions.Like(s.Provider, pattern)
                     || (s.Name != null && EF.Functions.Like(s.Name, pattern))
                     || (s.ServiceNumber != null && EF.Functions.Like(s.ServiceNumber, pattern))
                     || (s.PhoneNumber != null && EF.Functions.Like(s.PhoneNumber, pattern))
                     || (s.AccountNumber != null && EF.Functions.Like(s.AccountNumber, pattern))
                     || (s.Location != null && EF.Functions.Like(s.Location.Name, pattern))
                     || (telecomTypeMatch.HasValue && s.ServiceType == telecomTypeMatch.Value)))
            .Take(MaxResultsPerCategory)
            .ToListAsync())
            .Select(s => new TelecomServiceSearchHit
            {
                Id = s.Id,
                ServiceType = s.ServiceType.ToString(),
                Provider = s.Provider,
                ServiceNumber = s.ServiceNumber,
                Location = s.Location?.Name
            })
            .ToList();

        return result;
    }
}
