using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

/// <summary>
/// Reutiliza ClosedXML (ya usado por ExcelImportService para importar equipos) para generar los
/// reportes en .xlsx. Cada método aplica los filtros recibidos, escribe encabezados + filas,
/// autoajusta columnas y activa el autofiltro de Excel; nunca incluye columnas de contraseñas
/// o credenciales.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateDevicesReportAsync(ReportFilterViewModel filter)
    {
        var query = _context.Devices
            .Include(d => d.Department)
            .Include(d => d.Location)
            .Include(d => d.Assignments).ThenInclude(a => a.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (filter.LocationId.HasValue) query = query.Where(d => d.LocationId == filter.LocationId);
        if (filter.DepartmentId.HasValue) query = query.Where(d => d.DepartmentId == filter.DepartmentId);
        if (filter.DeviceType.HasValue) query = query.Where(d => d.DeviceType == filter.DeviceType);
        if (filter.DeviceStatus.HasValue) query = query.Where(d => d.Status == filter.DeviceStatus);

        var devices = await query.OrderBy(d => d.InventoryNumber).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Equipos");

        var headers = new[]
        {
            "Código de activo", "Tipo de equipo", "Marca", "Modelo", "Número de serie",
            "Nombre del equipo (hostname)", "Sistema operativo", "Usuario asignado", "Departamento",
            "Sucursal / ubicación", "Estado", "Fecha de asignación", "Fecha de registro", "Notas"
        };
        WriteHeaders(sheet, headers);

        var row = 2;
        foreach (var d in devices)
        {
            var current = d.CurrentAssignment;
            var col = 1;
            sheet.Cell(row, col++).Value = d.InventoryNumber;
            sheet.Cell(row, col++).Value = d.DeviceType.ToString();
            sheet.Cell(row, col++).Value = d.Brand ?? "";
            sheet.Cell(row, col++).Value = d.Model ?? "";
            sheet.Cell(row, col++).Value = d.SerialNumber ?? "";
            sheet.Cell(row, col++).Value = d.ComputerName ?? "";
            sheet.Cell(row, col++).Value = d.OperatingSystem ?? "";
            sheet.Cell(row, col++).Value = current != null ? $"{current.Employee.FirstName} {current.Employee.LastName}" : "";
            sheet.Cell(row, col++).Value = d.Department?.Name ?? "";
            sheet.Cell(row, col++).Value = d.Location?.Name ?? "";
            sheet.Cell(row, col++).Value = d.Status.ToString();
            sheet.Cell(row, col++).Value = current?.AssignedAt.ToLocalTime().ToString("dd/MM/yyyy") ?? "";
            sheet.Cell(row, col++).Value = d.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy");
            sheet.Cell(row, col).Value = d.Notes ?? "";
            row++;
        }

        FinalizeSheet(sheet, headers.Length, row - 1);
        return ToBytes(workbook);
    }

    public async Task<byte[]> GenerateEmployeesReportAsync(ReportFilterViewModel filter)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Location)
            .Include(e => e.DeviceAssignments.Where(a => a.ReturnedAt == null)).ThenInclude(a => a.Device)
            .AsNoTracking()
            .AsQueryable();

        if (filter.LocationId.HasValue) query = query.Where(e => e.LocationId == filter.LocationId);
        if (filter.DepartmentId.HasValue) query = query.Where(e => e.DepartmentId == filter.DepartmentId);

        var employees = await query.OrderBy(e => e.LastName).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Personas");

        var headers = new[]
        {
            "Nombre completo", "Número de empleado", "Puesto", "Departamento", "Sucursal / ubicación",
            "Correo", "Teléfono", "Estado", "Equipo asignado", "Código del equipo"
        };
        WriteHeaders(sheet, headers);

        var row = 2;
        foreach (var e in employees)
        {
            var assignment = e.DeviceAssignments.FirstOrDefault(a => a.ReturnedAt == null);
            var col = 1;
            sheet.Cell(row, col++).Value = e.FullName;
            sheet.Cell(row, col++).Value = e.EmployeeNumber;
            sheet.Cell(row, col++).Value = e.Position ?? "";
            sheet.Cell(row, col++).Value = e.Department?.Name ?? "";
            sheet.Cell(row, col++).Value = e.Location?.Name ?? "";
            sheet.Cell(row, col++).Value = e.Email;
            sheet.Cell(row, col++).Value = e.Phone ?? "";
            sheet.Cell(row, col++).Value = e.Status.ToString();
            sheet.Cell(row, col++).Value = assignment != null ? $"{assignment.Device.Brand} {assignment.Device.Model}".Trim() : "";
            sheet.Cell(row, col).Value = assignment?.Device.InventoryNumber ?? "";
            row++;
        }

        FinalizeSheet(sheet, headers.Length, row - 1);
        return ToBytes(workbook);
    }

    public async Task<byte[]> GenerateServicesReportAsync(ReportFilterViewModel filter)
    {
        var query = _context.TelecomServices
            .Include(s => s.Location)
            .AsNoTracking()
            .AsQueryable();

        if (filter.LocationId.HasValue) query = query.Where(s => s.LocationId == filter.LocationId);
        if (filter.ServiceType.HasValue) query = query.Where(s => s.ServiceType == filter.ServiceType);
        if (filter.ServiceStatus.HasValue) query = query.Where(s => s.IsActive == filter.ServiceStatus);

        var services = await query.OrderBy(s => s.ServiceType).ThenBy(s => s.Provider).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Servicios");

        var headers = new[]
        {
            "Tipo de servicio", "Proveedor", "Número de servicio", "Número telefónico", "Número de cuenta",
            "Sucursal", "Dirección", "Contacto del proveedor", "Teléfono de soporte", "Correo de soporte",
            "Estado", "Observaciones"
        };
        WriteHeaders(sheet, headers);

        var row = 2;
        foreach (var s in services)
        {
            var col = 1;
            sheet.Cell(row, col++).Value = s.ServiceType.DisplayName();
            sheet.Cell(row, col++).Value = s.Provider;
            sheet.Cell(row, col++).Value = s.ServiceNumber ?? "";
            sheet.Cell(row, col++).Value = s.PhoneNumber ?? "";
            sheet.Cell(row, col++).Value = s.AccountNumber ?? "";
            sheet.Cell(row, col++).Value = s.Location?.Name ?? "";
            sheet.Cell(row, col++).Value = s.Address ?? "";
            sheet.Cell(row, col++).Value = s.ProviderContact ?? "";
            sheet.Cell(row, col++).Value = s.SupportPhone ?? "";
            sheet.Cell(row, col++).Value = s.SupportEmail ?? "";
            sheet.Cell(row, col++).Value = s.IsActive ? "Activo" : "Inactivo";
            sheet.Cell(row, col).Value = s.Notes ?? "";
            row++;
        }

        FinalizeSheet(sheet, headers.Length, row - 1);
        return ToBytes(workbook);
    }

    public async Task<byte[]> GenerateGeneralReportAsync(ReportFilterViewModel filter)
    {
        var query = _context.Devices
            .Include(d => d.Department)
            .Include(d => d.Location)
            .Include(d => d.Assignments).ThenInclude(a => a.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (filter.LocationId.HasValue) query = query.Where(d => d.LocationId == filter.LocationId);
        if (filter.DepartmentId.HasValue) query = query.Where(d => d.DepartmentId == filter.DepartmentId);

        var devices = await query.OrderBy(d => d.InventoryNumber).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("General");

        var headers = new[] { "Empleado", "Departamento", "Sucursal", "Equipo", "Marca", "Modelo", "Serie", "Estado" };
        WriteHeaders(sheet, headers);

        var row = 2;
        foreach (var d in devices)
        {
            var current = d.CurrentAssignment;
            var col = 1;
            sheet.Cell(row, col++).Value = current != null ? $"{current.Employee.FirstName} {current.Employee.LastName}" : "Sin asignar";
            sheet.Cell(row, col++).Value = d.Department?.Name ?? "";
            sheet.Cell(row, col++).Value = d.Location?.Name ?? "";
            sheet.Cell(row, col++).Value = d.InventoryNumber;
            sheet.Cell(row, col++).Value = d.Brand ?? "";
            sheet.Cell(row, col++).Value = d.Model ?? "";
            sheet.Cell(row, col++).Value = d.SerialNumber ?? "";
            sheet.Cell(row, col).Value = d.Status.ToString();
            row++;
        }

        FinalizeSheet(sheet, headers.Length, row - 1);
        return ToBytes(workbook);
    }

    private static void WriteHeaders(IXLWorksheet sheet, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D1B3F");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void FinalizeSheet(IXLWorksheet sheet, int columnCount, int lastRow)
    {
        if (lastRow >= 1)
        {
            sheet.RangeUsed()?.SetAutoFilter();
        }
        sheet.SheetView.FreezeRows(1);
        sheet.Columns(1, columnCount).AdjustToContents();
    }

    private static byte[] ToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
