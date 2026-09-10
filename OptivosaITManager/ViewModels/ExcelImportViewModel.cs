namespace OptivosaITManager.ViewModels;

public enum ImportRowStatus
{
    Valido,
    Duplicado,
    Incompleto,
    Error
}

public class ImportRowResult
{
    public int RowNumber { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string? ComputerName { get; set; }
    public ImportRowStatus Status { get; set; }
    public string? Message { get; set; }
}

public class ExcelImportPreviewViewModel
{
    /// <summary>Identificador del archivo temporal guardado en el servidor, usado para confirmar la importación.</summary>
    public string ImportToken { get; set; } = string.Empty;

    public int TotalRows => Rows.Count;
    public int ValidCount => Rows.Count(r => r.Status == ImportRowStatus.Valido);
    public int DuplicateCount => Rows.Count(r => r.Status == ImportRowStatus.Duplicado);
    public int ErrorCount => Rows.Count(r => r.Status == ImportRowStatus.Error);
    public int IncompleteCount => Rows.Count(r => r.Status == ImportRowStatus.Incompleto);

    public List<ImportRowResult> Rows { get; set; } = new();
}
