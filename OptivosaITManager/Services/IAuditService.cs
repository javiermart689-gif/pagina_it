namespace OptivosaITManager.Services;

public static class AuditActions
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string CrearUsuario = "CrearUsuario";
    public const string ModificarUsuario = "ModificarUsuario";
    public const string CrearEquipo = "CrearEquipo";
    public const string ModificarEquipo = "ModificarEquipo";
    public const string DesactivarEquipo = "DesactivarEquipo";
    public const string AsignarEquipo = "AsignarEquipo";
    public const string ReasignarEquipo = "ReasignarEquipo";
    public const string RegistrarMantenimiento = "RegistrarMantenimiento";
    public const string ActualizarMantenimiento = "ActualizarMantenimiento";
    public const string ConsultarContrasena = "ConsultarContrasena";
    public const string CopiarContrasena = "CopiarContrasena";
    public const string CrearCredencial = "CrearCredencial";
    public const string ModificarCredencial = "ModificarCredencial";
    public const string EliminarCredencial = "EliminarCredencial";
    public const string CrearCorreo = "CrearCorreo";
    public const string ModificarCorreo = "ModificarCorreo";
    public const string ImportarExcel = "ImportarExcel";
}

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId, string? details = null);
}
