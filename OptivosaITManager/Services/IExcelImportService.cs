using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

public interface IExcelImportService
{
    /// <summary>
    /// Analiza el Excel completo (equipos, usuarios, asignaciones y accesos que pueda contener
    /// cada fila) sin escribir nada en la base de datos. <paramref name="importPasswords"/> debe
    /// venir en true solo si el usuario marcó explícitamente que quiere importar contraseñas
    /// detectadas en el archivo (ver PasswordColumnDetected).
    /// </summary>
    Task<ExcelImportPreviewViewModel> PreviewAsync(Stream fileStream, string fileName, bool importPasswords);

    /// <summary>
    /// Re-analiza el archivo guardado (contra el estado actual de la base de datos) y aplica los
    /// cambios dentro de una única transacción. Registra un ImportHistory con el resumen.
    /// </summary>
    Task<ExcelImportResultViewModel> ConfirmAsync(string importToken, bool importPasswords, string? performedByUserId);
}
