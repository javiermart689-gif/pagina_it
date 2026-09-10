using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

/// <summary>
/// Importación de equipos desde Excel (.xlsx). Se valida el archivo completo antes de guardar nada
/// en la base de datos; el usuario debe confirmar explícitamente la importación de los registros válidos.
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private static readonly string[] RequiredHeaders = { "InventoryNumber", "DeviceType", "Status" };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditService _auditService;

    public ExcelImportService(ApplicationDbContext context, IWebHostEnvironment environment, IAuditService auditService)
    {
        _context = context;
        _environment = environment;
        _auditService = auditService;
    }

    private string ImportsFolder
    {
        get
        {
            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "Imports");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    private sealed class ColumnMap
    {
        public int Inventory, Type, Brand, Model, Serial, ComputerName, Os, Status, Notes;
    }

    private static ColumnMap MapColumns(List<string> headers)
    {
        int Col(string name) => headers.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase)) + 1;
        return new ColumnMap
        {
            Inventory = Col("InventoryNumber"),
            Type = Col("DeviceType"),
            Brand = Col("Brand"),
            Model = Col("Model"),
            Serial = Col("SerialNumber"),
            ComputerName = Col("ComputerName"),
            Os = Col("OperatingSystem"),
            Status = Col("Status"),
            Notes = Col("Notes")
        };
    }

    private async Task<List<ImportRowResult>> ValidateAsync(IXLWorksheet worksheet, ColumnMap map)
    {
        var rows = new List<ImportRowResult>();
        var existingInventoryNumbers = (await _context.Devices.Select(d => d.InventoryNumber).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            string GetCell(int col) => col > 0 ? row.Cell(col).GetString().Trim() : string.Empty;

            var inventoryNumber = GetCell(map.Inventory);
            var deviceTypeText = GetCell(map.Type);
            var statusText = GetCell(map.Status);
            var serial = GetCell(map.Serial);
            var computerName = GetCell(map.ComputerName);

            var result = new ImportRowResult { RowNumber = rowNum, InventoryNumber = inventoryNumber, ComputerName = computerName };

            if (string.IsNullOrWhiteSpace(inventoryNumber))
            {
                result.Status = ImportRowStatus.Error;
                result.Message = "Número de inventario vacío.";
            }
            else if (!Enum.TryParse<DeviceType>(deviceTypeText, true, out _))
            {
                result.Status = ImportRowStatus.Error;
                result.Message = $"Tipo de equipo inválido: '{deviceTypeText}'.";
            }
            else if (!Enum.TryParse<DeviceStatus>(statusText, true, out _))
            {
                result.Status = ImportRowStatus.Error;
                result.Message = $"Estado inválido: '{statusText}'.";
            }
            else if (existingInventoryNumbers.Contains(inventoryNumber) || !seenInFile.Add(inventoryNumber))
            {
                result.Status = ImportRowStatus.Duplicado;
                result.Message = "Ya existe un equipo con este número de inventario.";
            }
            else if (string.IsNullOrWhiteSpace(serial) && string.IsNullOrWhiteSpace(computerName))
            {
                result.Status = ImportRowStatus.Incompleto;
                result.Message = "Faltan número de serie y nombre de equipo.";
            }
            else
            {
                result.Status = ImportRowStatus.Valido;
            }

            rows.Add(result);
        }

        return rows;
    }

    public async Task<ExcelImportPreviewViewModel> PreviewDevicesAsync(Stream fileStream)
    {
        var token = Guid.NewGuid().ToString("N");
        var savedPath = Path.Combine(ImportsFolder, $"{token}.xlsx");

        await using (var fileOnDisk = File.Create(savedPath))
        {
            await fileStream.CopyToAsync(fileOnDisk);
        }

        var preview = new ExcelImportPreviewViewModel { ImportToken = token };

        using var workbook = new XLWorkbook(savedPath);
        var worksheet = workbook.Worksheets.First();
        var headers = worksheet.Row(1).CellsUsed().Select(c => c.GetString().Trim()).ToList();

        var missingHeaders = RequiredHeaders.Except(headers, StringComparer.OrdinalIgnoreCase).ToList();
        if (missingHeaders.Count > 0)
        {
            preview.Rows.Add(new ImportRowResult
            {
                RowNumber = 1,
                Status = ImportRowStatus.Error,
                Message = $"Faltan columnas obligatorias: {string.Join(", ", missingHeaders)}"
            });
            return preview;
        }

        preview.Rows = await ValidateAsync(worksheet, MapColumns(headers));
        return preview;
    }

    public async Task<int> ConfirmDevicesImportAsync(string importToken)
    {
        var path = Path.Combine(ImportsFolder, $"{importToken}.xlsx");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("El archivo de importación ya no está disponible. Vuelva a cargarlo.");
        }

        var imported = 0;
        using (var workbook = new XLWorkbook(path))
        {
            var worksheet = workbook.Worksheets.First();
            var headers = worksheet.Row(1).CellsUsed().Select(c => c.GetString().Trim()).ToList();
            var map = MapColumns(headers);

            var rows = await ValidateAsync(worksheet, map);
            var validInventoryNumbers = rows
                .Where(r => r.Status == ImportRowStatus.Valido)
                .Select(r => r.InventoryNumber)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var rowNum = 2; rowNum <= lastRow; rowNum++)
            {
                var row = worksheet.Row(rowNum);
                if (row.IsEmpty()) continue;

                string GetCell(int col) => col > 0 ? row.Cell(col).GetString().Trim() : string.Empty;
                var inventoryNumber = GetCell(map.Inventory);
                if (!validInventoryNumbers.Contains(inventoryNumber)) continue;

                Enum.TryParse<DeviceType>(GetCell(map.Type), true, out var deviceType);
                Enum.TryParse<DeviceStatus>(GetCell(map.Status), true, out var status);

                _context.Devices.Add(new Device
                {
                    InventoryNumber = inventoryNumber,
                    DeviceType = deviceType,
                    Brand = GetCell(map.Brand),
                    Model = GetCell(map.Model),
                    SerialNumber = GetCell(map.Serial),
                    ComputerName = GetCell(map.ComputerName),
                    OperatingSystem = GetCell(map.Os),
                    Status = status,
                    Notes = GetCell(map.Notes)
                });
                imported++;
            }

            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync(AuditActions.ImportarExcel, nameof(Device), null, $"Importados {imported} equipos desde Excel.");

        File.Delete(path);

        return imported;
    }
}
