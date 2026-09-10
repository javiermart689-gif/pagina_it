using Microsoft.AspNetCore.Identity;

namespace OptivosaITManager.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
