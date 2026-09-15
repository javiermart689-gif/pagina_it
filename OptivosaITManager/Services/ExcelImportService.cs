using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

/// <summary>
/// Importación unificada desde Excel: una misma fila puede describir un Equipo, la Persona a la
/// que está asignado, la Asignación entre ambos, y un Acceso de correo — sin duplicar información
/// ya existente y sin mezclar todo en una entidad gigante (Device/Employee/DeviceAssignment/
/// AccessCredential siguen siendo entidades separadas; este servicio solo detecta y crea las
/// relaciones entre ellas). Ver PROVISIONAL: los alias de encabezado de <see cref="HeaderAliases"/>
/// son un mapeo de ejemplo — cuando se conozca el Excel real de Optivosa hay que revisarlos y
/// ajustarlos a sus columnas reales.
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditService _auditService;
    private readonly IAssignmentService _assignmentService;
    private readonly IEncryptionService _encryptionService;

    public ExcelImportService(ApplicationDbContext context, IWebHostEnvironment environment,
        IAuditService auditService, IAssignmentService assignmentService, IEncryptionService encryptionService)
    {
        _context = context;
        _environment = environment;
        _auditService = auditService;
        _assignmentService = assignmentService;
        _encryptionService = encryptionService;
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

    // PROVISIONAL: alias de encabezado por campo canónico. Cuando llegue el Excel real de
    // Optivosa hay que revisar sus columnas reales y ampliar/ajustar estas listas — no asumir
    // que coinciden con estos nombres de ejemplo.
    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["InventoryNumber"] = new[] { "InventoryNumber", "No. Inventario", "Numero de Inventario", "Número de Inventario", "Inventario", "No. Activo" },
        ["SerialNumber"] = new[] { "SerialNumber", "No. Serie", "Numero de Serie", "Número de Serie", "Serie" },
        ["ComputerName"] = new[] { "ComputerName", "Hostname", "Nombre del Equipo", "Equipo" },
        ["DeviceType"] = new[] { "DeviceType", "Tipo de Equipo", "Tipo" },
        ["Brand"] = new[] { "Brand", "Marca" },
        ["Model"] = new[] { "Model", "Modelo" },
        ["OperatingSystem"] = new[] { "OperatingSystem", "Sistema Operativo", "SO" },
        ["DeviceStatus"] = new[] { "Status", "Estado", "Estado del Equipo" },
        ["DeviceNotes"] = new[] { "Notes", "Notas", "Observaciones" },

        ["EmployeeNumber"] = new[] { "EmployeeNumber", "No. Empleado", "Numero de Empleado", "Número de Empleado" },
        ["FirstName"] = new[] { "FirstName", "Nombre", "Nombres" },
        ["LastName"] = new[] { "LastName", "Apellido", "Apellidos" },
        ["EmployeeFullName"] = new[] { "Usuario", "Usuario Asignado", "Persona", "Empleado", "Persona Asignada", "Nombre Completo" },
        ["Email"] = new[] { "Email", "Correo", "Correo Electronico", "Correo Electrónico", "Correo Corporativo" },
        ["Department"] = new[] { "Department", "Departamento", "Area", "Área" },
        ["Position"] = new[] { "Position", "Puesto", "Cargo" },
        ["Location"] = new[] { "Location", "Ubicacion", "Ubicación", "Sucursal" },
        ["Phone"] = new[] { "Phone", "Telefono", "Teléfono" },
        ["Extension"] = new[] { "Extension", "Extensión" },

        ["ServiceName"] = new[] { "Service", "Servicio", "Sistema" },
        ["Password"] = new[] { "Password", "Contraseña", "Contrasena", "Clave" },
    };

    private sealed class ColumnMap
    {
        public Dictionary<string, int> Columns { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int Get(string canonicalField) => Columns.TryGetValue(canonicalField, out var c) ? c : 0;
        public bool Has(string canonicalField) => Get(canonicalField) > 0;
    }

    private static ColumnMap MapColumns(List<string> headers)
    {
        var map = new ColumnMap();
        foreach (var (canonical, aliases) in HeaderAliases)
        {
            var idx = headers.FindIndex(h => aliases.Any(a => string.Equals(a, h, StringComparison.OrdinalIgnoreCase)));
            if (idx >= 0) map.Columns[canonical] = idx + 1;
        }
        return map;
    }

    public async Task<ExcelImportPreviewViewModel> PreviewAsync(Stream fileStream, string fileName, bool importPasswords)
    {
        var token = Guid.NewGuid().ToString("N");
        var savedPath = Path.Combine(ImportsFolder, $"{token}.xlsx");

        await using (var fileOnDisk = File.Create(savedPath))
        {
            await fileStream.CopyToAsync(fileOnDisk);
        }

        using var workbook = new XLWorkbook(savedPath);
        var worksheet = workbook.Worksheets.First();
        var headers = worksheet.Row(1).CellsUsed().Select(c => c.GetString().Trim()).ToList();
        var map = MapColumns(headers);

        var preview = await BuildPlanAsync(worksheet, map, importPasswords, apply: false, performedByUserId: null);
        preview.ImportToken = token;
        preview.FileName = fileName;
        preview.PasswordColumnDetected = map.Has("Password");
        preview.ImportPasswords = importPasswords && map.Has("Password");
        return preview;
    }

    public async Task<ExcelImportResultViewModel> ConfirmAsync(string importToken, bool importPasswords, string? performedByUserId)
    {
        var path = Path.Combine(ImportsFolder, $"{importToken}.xlsx");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("El archivo de importación ya no está disponible. Vuelva a cargarlo.");
        }

        var result = new ExcelImportResultViewModel();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            using (var workbook = new XLWorkbook(path))
            {
                var worksheet = workbook.Worksheets.First();
                var headers = worksheet.Row(1).CellsUsed().Select(c => c.GetString().Trim()).ToList();
                var map = MapColumns(headers);

                var plan = await BuildPlanAsync(worksheet, map, importPasswords, apply: true, performedByUserId, result);

                var history = new ImportHistory
                {
                    FileName = Path.GetFileName(path),
                    ImportedByUserId = performedByUserId,
                    ImportedAt = DateTime.UtcNow,
                    TotalRows = plan.TotalRows,
                    DevicesCreated = result.DevicesCreated,
                    DevicesUpdated = result.DevicesUpdated,
                    EmployeesCreated = result.EmployeesCreated,
                    EmployeesUpdated = result.EmployeesUpdated,
                    AssignmentsCreated = result.AssignmentsCreated,
                    AssignmentsReassigned = result.AssignmentsReassigned,
                    AccessesCreated = result.AccessesCreated,
                    AccessesUpdated = result.AccessesUpdated,
                    SkippedRows = result.Skipped,
                    ErrorRows = result.Errors
                };
                _context.ImportHistories.Add(history);
                await _context.SaveChangesAsync();
            }

            await _auditService.LogAsync(AuditActions.ImportarExcel, nameof(ImportHistory), null,
                $"Importación desde '{Path.GetFileName(path)}': {result.DevicesCreated} equipos nuevos, " +
                $"{result.EmployeesCreated} usuarios nuevos, {result.AssignmentsCreated} asignaciones nuevas, " +
                $"{result.AssignmentsReassigned} reasignaciones, {result.AccessesCreated} accesos nuevos.");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        File.Delete(path);
        return result;
    }

    /// <summary>
    /// Recorre todas las filas UNA vez y decide qué hacer con cada Equipo/Usuario/Asignación/
    /// Acceso implícito en ellas. Con <paramref name="apply"/> = false solo simula (para la vista
    /// previa, sin tocar la base de datos); con true, además escribe los cambios y acumula los
    /// contadores en <paramref name="result"/>.
    /// </summary>
    private async Task<ExcelImportPreviewViewModel> BuildPlanAsync(IXLWorksheet worksheet, ColumnMap map,
        bool importPasswords, bool apply, string? performedByUserId, ExcelImportResultViewModel? result = null)
    {
        var preview = new ExcelImportPreviewViewModel();
        importPasswords = importPasswords && map.Has("Password");

        // --- Snapshots existentes (para resolver "¿ya existe?") ---
        var devicesByInventory = await _context.Devices.ToDictionaryAsync(d => d.InventoryNumber, d => d, StringComparer.OrdinalIgnoreCase);
        var devicesBySerial = await _context.Devices.Where(d => d.SerialNumber != null)
            .GroupBy(d => d.SerialNumber!).ToDictionaryAsync(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var devicesByComputerName = await _context.Devices.Where(d => d.ComputerName != null)
            .GroupBy(d => d.ComputerName!).ToDictionaryAsync(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var employeesByNumber = await _context.Employees.ToDictionaryAsync(e => e.EmployeeNumber, e => e, StringComparer.OrdinalIgnoreCase);
        var employeesByEmail = await _context.Employees.Where(e => e.Email != null)
            .GroupBy(e => e.Email).ToDictionaryAsync(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        // Solo para deduplicar dentro del mismo archivo cuando la fila no trae número ni correo.
        var employeesByFullNameInFile = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);

        var departments = await _context.Departments.ToDictionaryAsync(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);
        var locations = await _context.Locations.ToDictionaryAsync(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        var accessesByUsername = await _context.Accesses.Where(a => a.Type == AccessType.Correo)
            .GroupBy(a => a.Username).ToDictionaryAsync(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Asignación activa por equipo, indexada por la instancia de Device (no por Id: un equipo
        // nuevo de este mismo archivo no tiene Id todavía en modo vista previa). Como todas las
        // consultas de esta pasada comparten el mismo ApplicationDbContext con seguimiento de
        // cambios activado, EF Core devuelve la MISMA instancia para un equipo ya visto arriba
        // (devicesByInventory/BySerial/ByComputerName), así que sirve como clave estable.
        var currentAssignmentByDevice = new Dictionary<Device, DeviceAssignment>();
        foreach (var a in await _context.DeviceAssignments.Where(a => a.ReturnedAt == null)
                     .Include(a => a.Employee).Include(a => a.Device).ToListAsync())
        {
            currentAssignmentByDevice[a.Device] = a;
        }

        var nextEmployeeNumberSeq = 1 + employeesByNumber.Keys
            .Where(n => n.StartsWith("IMP-", StringComparison.OrdinalIgnoreCase))
            .Select(n => int.TryParse(n[4..], out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        // Para el reporte de duplicados dentro del archivo (sección 10).
        var inventoryRowsSeen = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        var serialRowsSeen = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        string GetCell(IXLRow row, string field)
        {
            var col = map.Get(field);
            return col > 0 ? row.Cell(col).GetString().Trim() : string.Empty;
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var processedRows = 0;

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;
            processedRows++;

            var inventoryNumber = GetCell(row, "InventoryNumber");
            var serialNumber = GetCell(row, "SerialNumber");
            var computerName = GetCell(row, "ComputerName");
            // No solo los identificadores: si la fila trae marca/modelo/tipo/etc. pero olvidó
            // el identificador, igual debe tratarse como "hay un equipo aquí" para reportar el
            // error correspondiente, en vez de ignorar la fila silenciosamente.
            var hasDeviceData = inventoryNumber.Length > 0 || serialNumber.Length > 0 || computerName.Length > 0
                || GetCell(row, "Brand").Length > 0 || GetCell(row, "Model").Length > 0
                || GetCell(row, "DeviceType").Length > 0 || GetCell(row, "OperatingSystem").Length > 0;

            var firstName = GetCell(row, "FirstName");
            var lastName = GetCell(row, "LastName");
            var fullName = GetCell(row, "EmployeeFullName");
            var email = GetCell(row, "Email");
            var employeeNumber = GetCell(row, "EmployeeNumber");
            var hasEmployeeData = firstName.Length > 0 || lastName.Length > 0 || fullName.Length > 0 || email.Length > 0;

            Device? device = null;

            // ---------- 1. EQUIPO ----------
            if (hasDeviceData)
            {
                if (inventoryNumber.Length == 0 && serialNumber.Length == 0 && computerName.Length == 0)
                {
                    preview.RowErrors.Add($"Fila {rowNum}: no se encontró número de inventario, número de serie ni nombre de equipo.");
                }
                else if (inventoryNumber.Length == 0)
                {
                    preview.RowErrors.Add($"Fila {rowNum}: falta número de inventario (identificador principal del equipo).");
                }
                else
                {
                    if (!inventoryRowsSeen.TryGetValue(inventoryNumber, out var invRows)) inventoryRowsSeen[inventoryNumber] = invRows = new();
                    invRows.Add(rowNum);
                    if (serialNumber.Length > 0)
                    {
                        if (!serialRowsSeen.TryGetValue(serialNumber, out var serRows)) serialRowsSeen[serialNumber] = serRows = new();
                        serRows.Add(rowNum);
                    }

                    device = devicesByInventory.GetValueOrDefault(inventoryNumber)
                        ?? (serialNumber.Length > 0 ? devicesBySerial.GetValueOrDefault(serialNumber) : null)
                        ?? (computerName.Length > 0 ? devicesByComputerName.GetValueOrDefault(computerName) : null);

                    var devicePlan = new ImportDevicePlan { RowNumber = rowNum, InventoryNumber = inventoryNumber };

                    if (device is null)
                    {
                        device = new Device
                        {
                            InventoryNumber = inventoryNumber,
                            SerialNumber = NullIfEmpty(serialNumber),
                            ComputerName = NullIfEmpty(computerName),
                            Brand = NullIfEmpty(GetCell(row, "Brand")),
                            Model = NullIfEmpty(GetCell(row, "Model")),
                            OperatingSystem = NullIfEmpty(GetCell(row, "OperatingSystem")),
                            Notes = NullIfEmpty(GetCell(row, "DeviceNotes")),
                            Status = DeviceStatus.Disponible,
                            CreatedAt = DateTime.UtcNow,
                            QrToken = Guid.NewGuid().ToString("N")
                        };

                        var typeText = GetCell(row, "DeviceType");
                        device.DeviceType = Enum.TryParse<DeviceType>(typeText, true, out var dt) ? dt : DeviceType.Otro;

                        var statusText = GetCell(row, "DeviceStatus");
                        if (Enum.TryParse<DeviceStatus>(statusText, true, out var st)) device.Status = st;

                        ResolveDepartmentAndLocation(row, map, departments, locations, apply,
                            out var dept, out var loc, devicePlan.Changes);
                        device.Department = dept;
                        device.Location = loc;

                        devicePlan.Action = ImportEntityAction.Nuevo;

                        if (apply)
                        {
                            _context.Devices.Add(device);
                            await _context.SaveChangesAsync();
                            if (result != null) result.DevicesCreated++;
                        }
                        devicesByInventory[inventoryNumber] = device;
                        if (serialNumber.Length > 0) devicesBySerial[serialNumber] = device;
                        if (computerName.Length > 0) devicesByComputerName[computerName] = device;
                    }
                    else
                    {
                        // Existente: NO sobrescribir automáticamente, solo detectar y listar cambios.
                        CompareAndMaybeApply(devicePlan.Changes, "Marca", device.Brand, NullIfEmpty(GetCell(row, "Brand")),
                            apply, v => device.Brand = v);
                        CompareAndMaybeApply(devicePlan.Changes, "Modelo", device.Model, NullIfEmpty(GetCell(row, "Model")),
                            apply, v => device.Model = v);
                        CompareAndMaybeApply(devicePlan.Changes, "Número de serie", device.SerialNumber, NullIfEmpty(serialNumber),
                            apply, v => device.SerialNumber = v);
                        CompareAndMaybeApply(devicePlan.Changes, "Hostname", device.ComputerName, NullIfEmpty(computerName),
                            apply, v => device.ComputerName = v);
                        CompareAndMaybeApply(devicePlan.Changes, "Sistema operativo", device.OperatingSystem, NullIfEmpty(GetCell(row, "OperatingSystem")),
                            apply, v => device.OperatingSystem = v);

                        devicePlan.Action = devicePlan.Changes.Count > 0 ? ImportEntityAction.Actualizar : ImportEntityAction.SinCambios;

                        if (apply && devicePlan.Changes.Count > 0)
                        {
                            device.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            if (result != null) result.DevicesUpdated++;
                        }
                    }

                    preview.Devices.Add(devicePlan);
                }
            }

            // ---------- 2. USUARIO ----------
            Employee? employee = null;
            if (hasEmployeeData)
            {
                string resolvedFirst = firstName, resolvedLast = lastName;
                if (resolvedFirst.Length == 0 && resolvedLast.Length == 0 && fullName.Length > 0)
                {
                    var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    resolvedFirst = parts[0];
                    resolvedLast = parts.Length > 1 ? parts[1] : parts[0];
                }

                if (resolvedFirst.Length == 0 && resolvedLast.Length == 0 && email.Length == 0)
                {
                    preview.RowErrors.Add($"Fila {rowNum}: no se encontró nombre ni correo para identificar al usuario.");
                }
                else
                {
                    employee = (employeeNumber.Length > 0 ? employeesByNumber.GetValueOrDefault(employeeNumber) : null)
                        ?? (email.Length > 0 ? employeesByEmail.GetValueOrDefault(email) : null);

                    var nameKey = $"{resolvedFirst}|{resolvedLast}";
                    if (employee is null && employeeNumber.Length == 0 && email.Length == 0)
                    {
                        employee = employeesByFullNameInFile.GetValueOrDefault(nameKey);
                    }

                    // Si la fila solo trae correo (sin nombre en ninguna forma), se usa la parte
                    // local del correo como marcador de nombre en vez de dejarlo vacío.
                    if (resolvedFirst.Length == 0 && resolvedLast.Length == 0 && email.Length > 0)
                    {
                        resolvedFirst = email.Split('@')[0];
                        resolvedLast = "(pendiente)";
                    }

                    var displayName = $"{resolvedFirst} {resolvedLast}".Trim();
                    var employeePlan = new ImportEmployeePlan { RowNumber = rowNum, DisplayName = displayName };

                    if (employee is null)
                    {
                        var generatedNumber = employeeNumber.Length == 0;
                        employee = new Employee
                        {
                            EmployeeNumber = employeeNumber.Length > 0 ? employeeNumber : $"IMP-{nextEmployeeNumberSeq++:D6}",
                            FirstName = resolvedFirst.Length > 0 ? resolvedFirst : displayName,
                            LastName = resolvedLast.Length > 0 ? resolvedLast : resolvedFirst,
                            Email = email.Length > 0 ? email : $"pendiente+{Guid.NewGuid():N}@optivosa.local",
                            Position = NullIfEmpty(GetCell(row, "Position")),
                            Phone = NullIfEmpty(GetCell(row, "Phone")),
                            Extension = NullIfEmpty(GetCell(row, "Extension")),
                            Status = EmployeeStatus.Activo,
                            CreatedAt = DateTime.UtcNow
                        };

                        ResolveDepartmentAndLocation(row, map, departments, locations, apply,
                            out var dept, out var loc, employeePlan.Changes);
                        employee.Department = dept;
                        employee.Location = loc;

                        employeePlan.Action = ImportEntityAction.Nuevo;
                        employeePlan.GeneratedEmployeeNumber = generatedNumber;

                        if (apply)
                        {
                            _context.Employees.Add(employee);
                            await _context.SaveChangesAsync();
                            if (result != null) result.EmployeesCreated++;
                        }

                        if (employeeNumber.Length > 0) employeesByNumber[employeeNumber] = employee;
                        if (email.Length > 0) employeesByEmail[email] = employee;
                        employeesByFullNameInFile[nameKey] = employee;
                    }
                    else
                    {
                        CompareAndMaybeApply(employeePlan.Changes, "Puesto", employee.Position, NullIfEmpty(GetCell(row, "Position")),
                            apply, v => employee.Position = v);
                        CompareAndMaybeApply(employeePlan.Changes, "Teléfono", employee.Phone, NullIfEmpty(GetCell(row, "Phone")),
                            apply, v => employee.Phone = v);
                        CompareAndMaybeApply(employeePlan.Changes, "Extensión", employee.Extension, NullIfEmpty(GetCell(row, "Extension")),
                            apply, v => employee.Extension = v);

                        var deptText = GetCell(row, "Department");
                        if (deptText.Length > 0 && !string.Equals(employee.Department?.Name, deptText, StringComparison.OrdinalIgnoreCase))
                        {
                            employeePlan.Changes.Add(new ImportFieldChange { Field = "Departamento", OldValue = employee.Department?.Name, NewValue = deptText });
                            if (apply) employee.Department = ResolveOrCreate(departments, deptText, name => new Department { Name = name });
                        }

                        var locText = GetCell(row, "Location");
                        if (locText.Length > 0 && !string.Equals(employee.Location?.Name, locText, StringComparison.OrdinalIgnoreCase))
                        {
                            employeePlan.Changes.Add(new ImportFieldChange { Field = "Ubicación", OldValue = employee.Location?.Name, NewValue = locText });
                            if (apply) employee.Location = ResolveOrCreate(locations, locText, name => new Location { Name = name });
                        }

                        employeePlan.Action = employeePlan.Changes.Count > 0 ? ImportEntityAction.Actualizar : ImportEntityAction.SinCambios;

                        if (apply && employeePlan.Changes.Count > 0)
                        {
                            employee.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            if (result != null) result.EmployeesUpdated++;
                        }
                    }

                    preview.Employees.Add(employeePlan);
                }
            }

            // ---------- 3. ASIGNACIÓN ----------
            if (device != null && employee != null)
            {
                currentAssignmentByDevice.TryGetValue(device, out var current);

                // Se compara por referencia de objeto, no por Id: en vista previa un empleado
                // nuevo todavía no tiene Id real (queda en 0), así que comparar por Id podría
                // confundir a dos personas distintas creadas dentro del mismo archivo.
                if (current != null && current.Employee == employee)
                {
                    preview.Assignments.Add(new ImportAssignmentPlan
                    {
                        RowNumber = rowNum,
                        InventoryNumber = device.InventoryNumber,
                        EmployeeDisplayName = employee.FirstName + " " + employee.LastName,
                        Action = AssignmentPlanAction.SinCambios
                    });
                }
                else
                {
                    var plan = new ImportAssignmentPlan
                    {
                        RowNumber = rowNum,
                        InventoryNumber = device.InventoryNumber,
                        EmployeeDisplayName = employee.FirstName + " " + employee.LastName,
                        PreviousEmployeeDisplayName = current != null ? $"{current.Employee?.FirstName} {current.Employee?.LastName}" : null,
                        Action = current != null ? AssignmentPlanAction.Reasignacion : AssignmentPlanAction.Nueva
                    };
                    preview.Assignments.Add(plan);

                    if (apply)
                    {
                        var assignResult = await _assignmentService.AssignOrReassignAsync(device, employee.Id, performedByUserId);
                        assignResult.Assignment.Employee = employee;
                        currentAssignmentByDevice[device] = assignResult.Assignment;
                        if (result != null)
                        {
                            if (assignResult.Action == AssignmentAction.Created) result.AssignmentsCreated++;
                            else if (assignResult.Action == AssignmentAction.Reassigned) result.AssignmentsReassigned++;
                        }
                    }
                    else
                    {
                        // Solo para que las siguientes filas de la MISMA vista previa vean el
                        // nuevo dueño simulado (sin tocar la base de datos).
                        var simulated = new DeviceAssignment { DeviceId = device.Id, EmployeeId = employee.Id, Employee = employee, AssignedAt = DateTime.UtcNow };
                        currentAssignmentByDevice[device] = simulated;
                    }
                }
            }

            // ---------- 4. ACCESO DE CORREO ----------
            if (email.Length > 0)
            {
                var access = accessesByUsername.GetValueOrDefault(email);
                var accessPlan = new ImportAccessPlan { RowNumber = rowNum, Username = email };
                var passwordText = importPasswords ? GetCell(row, "Password") : string.Empty;

                if (access is null)
                {
                    var serviceName = NullIfEmpty(GetCell(row, "ServiceName")) ?? "Correo";
                    var displayName = employee != null ? $"{employee.FirstName} {employee.LastName} - {serviceName}" : email;

                    access = new AccessCredential
                    {
                        Name = displayName,
                        Type = AccessType.Correo,
                        Username = email,
                        EmployeeId = employee?.Id,
                        Employee = employee,
                        ServiceName = serviceName,
                        Status = AccessStatus.Activo,
                        EncryptedPassword = passwordText.Length > 0 ? _encryptionService.Encrypt(passwordText) : null,
                        CreatedAt = DateTime.UtcNow
                    };

                    accessPlan.Action = ImportEntityAction.Nuevo;

                    if (apply)
                    {
                        _context.Accesses.Add(access);
                        await _context.SaveChangesAsync();
                        if (result != null) result.AccessesCreated++;
                    }
                    accessesByUsername[email] = access;
                }
                else
                {
                    accessPlan.Action = ImportEntityAction.SinCambios;
                    if (passwordText.Length > 0 && access.EncryptedPassword != null)
                    {
                        accessPlan.Note = "Se detectó una contraseña para un acceso que ya existe; no se sobrescribe automáticamente.";
                    }
                    else if (passwordText.Length > 0 && access.EncryptedPassword is null)
                    {
                        accessPlan.Note = "Se completó la contraseña de un acceso existente que no la tenía.";
                        accessPlan.Action = ImportEntityAction.Actualizar;
                        if (apply)
                        {
                            access.EncryptedPassword = _encryptionService.Encrypt(passwordText);
                            access.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            if (result != null) result.AccessesUpdated++;
                        }
                    }
                }

                preview.Accesses.Add(accessPlan);
            }

            if (!hasDeviceData && !hasEmployeeData)
            {
                if (result != null) result.Skipped++;
            }
        }

        preview.TotalRows = processedRows;
        if (result != null) result.Errors = preview.RowErrors.Count;

        foreach (var (inv, rows) in inventoryRowsSeen.Where(kv => kv.Value.Count > 1))
        {
            preview.Duplicates.Add(new ImportDuplicateWarning { IdentifierLabel = "Número de inventario", Value = inv, RowNumbers = rows });
        }
        foreach (var (serial, rows) in serialRowsSeen.Where(kv => kv.Value.Count > 1))
        {
            preview.Duplicates.Add(new ImportDuplicateWarning { IdentifierLabel = "Número de serie", Value = serial, RowNumbers = rows });
        }

        return preview;
    }

    private static void ResolveDepartmentAndLocation(IXLRow row, ColumnMap map,
        Dictionary<string, Department> departments, Dictionary<string, Location> locations, bool apply,
        out Department? department, out Location? location, List<ImportFieldChange> changes)
    {
        department = null;
        location = null;

        var deptCol = map.Get("Department");
        if (deptCol > 0)
        {
            var deptText = row.Cell(deptCol).GetString().Trim();
            if (deptText.Length > 0)
            {
                var isNew = !departments.ContainsKey(deptText);
                department = ResolveOrCreate(departments, deptText, name => new Department { Name = name });
                if (isNew) changes.Add(new ImportFieldChange { Field = "Departamento", OldValue = null, NewValue = $"{deptText} (nuevo)" });
            }
        }

        var locCol = map.Get("Location");
        if (locCol > 0)
        {
            var locText = row.Cell(locCol).GetString().Trim();
            if (locText.Length > 0)
            {
                var isNew = !locations.ContainsKey(locText);
                location = ResolveOrCreate(locations, locText, name => new Location { Name = name });
                if (isNew) changes.Add(new ImportFieldChange { Field = "Ubicación", OldValue = null, NewValue = $"{locText} (nueva)" });
            }
        }
    }

    private static T ResolveOrCreate<T>(Dictionary<string, T> dict, string name, Func<string, T> factory)
    {
        if (dict.TryGetValue(name, out var existing)) return existing;
        var created = factory(name);
        dict[name] = created;
        return created;
    }

    private static void CompareAndMaybeApply(List<ImportFieldChange> changes, string label,
        string? oldValue, string? newValue, bool apply, Action<string?> setter)
    {
        // Nunca "borrar" un dato existente porque la celda del Excel vino vacía: solo se
        // considera un cambio real cuando el Excel trae un valor distinto y no vacío.
        if (newValue is null) return;
        if (string.Equals(oldValue, newValue, StringComparison.OrdinalIgnoreCase)) return;

        changes.Add(new ImportFieldChange { Field = label, OldValue = oldValue, NewValue = newValue });
        if (apply) setter(newValue);
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
