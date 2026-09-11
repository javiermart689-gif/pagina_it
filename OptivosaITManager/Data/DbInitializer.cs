using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Models;
using OptivosaITManager.Security;

namespace OptivosaITManager.Data;

/// <summary>
/// Crea roles, el usuario administrador inicial y catálogos básicos si la base de datos está vacía.
/// Se ejecuta una vez al iniciar la aplicación, después de aplicar migraciones.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminEmail = configuration["InitialAdmin:Email"] ?? "admin@optivosa.local";
        var adminPassword = configuration["InitialAdmin:Password"];

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "No se encontró InitialAdmin:Password para crear el usuario administrador inicial. " +
                    "Configúrelo mediante User Secrets (desarrollo) o la variable de entorno InitialAdmin__Password (producción). " +
                    "Consulte el README.");
            }

            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Administrador TI",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Administrador);
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear el usuario administrador inicial: {errors}");
            }
        }

        if (!await context.Departments.AnyAsync())
        {
            context.Departments.AddRange(
                new Department { Name = "TI" },
                new Department { Name = "Contabilidad" },
                new Department { Name = "Recursos Humanos" },
                new Department { Name = "Ventas" },
                new Department { Name = "Compras" },
                new Department { Name = "Operaciones" });
        }
        else
        {
            // Corrección no destructiva para instalaciones que ya sembraron el departamento con
            // el nombre antiguo "IT": se actualiza el mismo registro (mismo Id, mismas relaciones
            // con empleados/equipos) a "TI", nunca se borra ni se crea uno nuevo.
            var itDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Name == "IT");
            if (itDepartment is not null)
            {
                itDepartment.Name = "TI";
            }
        }

        if (!await context.Locations.AnyAsync())
        {
            context.Locations.AddRange(
                new Location { Name = "Mérida" },
                new Location { Name = "Oficina Central" },
                new Location { Name = "Bodega" },
                new Location { Name = "Sucursal" },
                new Location { Name = "Almacén" });
        }

        await context.SaveChangesAsync();
    }
}
