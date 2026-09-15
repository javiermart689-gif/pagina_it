namespace OptivosaITManager.ViewModels;

/// <summary>
/// Qué haría el importador con una entidad concreta (equipo/usuario) de una fila dada.
/// </summary>
public enum ImportEntityAction
{
    Nuevo,
    SinCambios,
    Actualizar,
    Error
}

public class ImportFieldChange
{
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class ImportDevicePlan
{
    public int RowNumber { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public ImportEntityAction Action { get; set; }
    public List<ImportFieldChange> Changes { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ImportEmployeePlan
{
    public int RowNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public ImportEntityAction Action { get; set; }
    public List<ImportFieldChange> Changes { get; set; } = new();
    public string? ErrorMessage { get; set; }
    /// <summary>True si el número de empleado no vino en el Excel y el sistema lo generó (p. ej. "IMP-000123").</summary>
    public bool GeneratedEmployeeNumber { get; set; }
}

public enum AssignmentPlanAction
{
    Nueva,
    Reasignacion,
    SinCambios
}

public class ImportAssignmentPlan
{
    public int RowNumber { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string EmployeeDisplayName { get; set; } = string.Empty;
    public string? PreviousEmployeeDisplayName { get; set; }
    public AssignmentPlanAction Action { get; set; }
}

public class ImportAccessPlan
{
    public int RowNumber { get; set; }
    public string Username { get; set; } = string.Empty;
    public ImportEntityAction Action { get; set; }
    /// <summary>Nota informativa, p. ej. cuando se detectó una contraseña para un acceso ya existente y no se sobrescribió.</summary>
    public string? Note { get; set; }
}

public class ImportDuplicateWarning
{
    public string IdentifierLabel { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public List<int> RowNumbers { get; set; } = new();
}

/// <summary>
/// Resultado de analizar el Excel (vista previa) o de haberlo aplicado (confirmación). El mismo
/// modelo sirve para ambos casos: en la vista previa refleja lo que el sistema HARÍA; después de
/// confirmar, refleja lo que efectivamente se hizo.
/// </summary>
public class ExcelImportPreviewViewModel
{
    public string ImportToken { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }

    /// <summary>True si el Excel tiene una columna que parece contener contraseñas (ver sección 7).</summary>
    public bool PasswordColumnDetected { get; set; }

    /// <summary>Si el usuario marcó la casilla "Importar contraseñas" al subir el archivo.</summary>
    public bool ImportPasswords { get; set; }

    public List<ImportDevicePlan> Devices { get; set; } = new();
    public List<ImportEmployeePlan> Employees { get; set; } = new();
    public List<ImportAssignmentPlan> Assignments { get; set; } = new();
    public List<ImportAccessPlan> Accesses { get; set; } = new();
    public List<ImportDuplicateWarning> Duplicates { get; set; } = new();

    /// <summary>Errores de fila que impiden procesar esa fila por completo (ej. sin identificador de equipo).</summary>
    public List<string> RowErrors { get; set; } = new();

    public int DevicesNew => Devices.Count(d => d.Action == ImportEntityAction.Nuevo);
    public int DevicesExisting => Devices.Count(d => d.Action == ImportEntityAction.SinCambios);
    public int DevicesChanged => Devices.Count(d => d.Action == ImportEntityAction.Actualizar);

    public int EmployeesNew => Employees.Count(e => e.Action == ImportEntityAction.Nuevo);
    public int EmployeesExisting => Employees.Count(e => e.Action == ImportEntityAction.SinCambios);
    public int EmployeesChanged => Employees.Count(e => e.Action == ImportEntityAction.Actualizar);

    public int AssignmentsNew => Assignments.Count(a => a.Action == AssignmentPlanAction.Nueva);
    public int AssignmentsReassigned => Assignments.Count(a => a.Action == AssignmentPlanAction.Reasignacion);

    public int AccessesNew => Accesses.Count(a => a.Action == ImportEntityAction.Nuevo);
    public int AccessesExisting => Accesses.Count(a => a.Action == ImportEntityAction.SinCambios);
    public int AccessesChanged => Accesses.Count(a => a.Action == ImportEntityAction.Actualizar);

    public int ErrorCount => RowErrors.Count;
    public int DuplicateCount => Duplicates.Count;

    /// <summary>True si hay al menos algo que crear o actualizar (para decidir si mostrar "Confirmar importación").</summary>
    public bool HasAnythingToImport =>
        DevicesNew + DevicesChanged + EmployeesNew + EmployeesChanged +
        AssignmentsNew + AssignmentsReassigned + AccessesNew + AccessesChanged > 0;
}

/// <summary>Resumen mostrado al terminar de confirmar una importación (sección 14).</summary>
public class ExcelImportResultViewModel
{
    public int DevicesCreated { get; set; }
    public int DevicesUpdated { get; set; }
    public int EmployeesCreated { get; set; }
    public int EmployeesUpdated { get; set; }
    public int AssignmentsCreated { get; set; }
    public int AssignmentsReassigned { get; set; }
    public int AccessesCreated { get; set; }
    public int AccessesUpdated { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
}
