using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.Security;
using OptivosaITManager.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'ConnectionStrings:DefaultConnection' (o está vacía). " +
        "Configúrela mediante la variable de entorno ConnectionStrings__DefaultConnection " +
        "(doble guion bajo) o mediante User Secrets en desarrollo. " +
        "Nota: appsettings.json define esta clave con un valor vacío por diseño (no debe contener " +
        "secretos); si esta excepción aparece en un despliegue, la variable de entorno no está " +
        "llegando al proceso — revise que esté definida en el servicio correcto y sin saltos de línea.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection("Encryption"));
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

var app = builder.Build();

// Las migraciones se aplican manualmente con `dotnet ef database update` (ver README),
// nunca automáticamente al iniciar, para evitar cambios de esquema no controlados en producción.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // AUTO_MIGRATE=1 es un modo opt-in (por defecto desactivado) pensado para entornos
        // de prueba efímeros (p. ej. un contenedor SQL Server temporal en Railway) donde no
        // hay forma de ejecutar `dotnet ef database update` manualmente contra la base de
        // datos. En el resto de entornos el comportamiento no cambia: nunca se migra sola.
        if (builder.Configuration["AUTO_MIGRATE"] == "1")
        {
            await context.Database.MigrateAsync();
            await DbInitializer.SeedAsync(scope.ServiceProvider, app.Configuration);
        }
        else if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            logger.LogWarning(
                "Hay migraciones pendientes. Ejecute 'dotnet ef database update' antes de usar la aplicación.");
        }
        else
        {
            await DbInitializer.SeedAsync(scope.ServiceProvider, app.Configuration);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "No se pudo verificar/inicializar la base de datos al arrancar. " +
            "Verifique la cadena de conexión y que las migraciones se hayan aplicado.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
