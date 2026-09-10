using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.ViewModels;

public class CatalogItemFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool LockedOut { get; set; }
}

public class UserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> AllRoles { get; set; } = new();
    public List<string> SelectedRoles { get; set; } = new();
}

public class CreateUserViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Nombre para mostrar")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    public List<string> AllRoles { get; set; } = new();
}
