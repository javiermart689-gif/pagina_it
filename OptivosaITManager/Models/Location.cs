using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public class Location
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<TelecomService> TelecomServices { get; set; } = new List<TelecomService>();
}
