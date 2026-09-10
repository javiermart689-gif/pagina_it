namespace OptivosaITManager.ViewModels;

public class DashboardViewModel
{
    public int TotalDevices { get; set; }
    public int AssignedDevices { get; set; }
    public int AvailableDevices { get; set; }
    public int MaintenanceDevices { get; set; }
    public int RetiredDevices { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalEmailAccounts { get; set; }

    public List<(string Label, int Count)> DevicesByStatus { get; set; } = new();
    public List<(string Label, int Count)> DevicesByType { get; set; } = new();
    public List<(string Label, int Count)> DevicesByDepartment { get; set; } = new();

    public List<RecentActivityItem> RecentActivity { get; set; } = new();
}

public class RecentActivityItem
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? UserName { get; set; }
}
