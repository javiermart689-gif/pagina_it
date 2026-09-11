namespace OptivosaITManager.ViewModels;

public class SearchResultViewModel
{
    public List<EmployeeSearchHit> Employees { get; set; } = new();
    public List<DeviceSearchHit> Devices { get; set; } = new();
    public List<EmailSearchHit> Emails { get; set; } = new();
}

public class EmployeeSearchHit
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Location { get; set; }
    public string? Phone { get; set; }
    public string? AssignedDeviceInventoryNumber { get; set; }
}

public class DeviceSearchHit
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string InventoryNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class EmailSearchHit
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
}
