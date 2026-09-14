using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public class Employee
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Position { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    [Required, StringLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(10)]
    public string? Extension { get; set; }

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Activo;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DeviceAssignment> DeviceAssignments { get; set; } = new List<DeviceAssignment>();
    public ICollection<AccessCredential> Accesses { get; set; } = new List<AccessCredential>();

    public string FullName => $"{FirstName} {LastName}";
}
