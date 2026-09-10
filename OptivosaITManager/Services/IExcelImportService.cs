using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

public interface IExcelImportService
{
    Task<ExcelImportPreviewViewModel> PreviewDevicesAsync(Stream fileStream);

    Task<int> ConfirmDevicesImportAsync(string importToken);
}
