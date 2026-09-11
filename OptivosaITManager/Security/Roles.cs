namespace OptivosaITManager.Security;

/// <summary>
/// El sistema tiene únicamente dos roles:
/// - SistemasTI ("Sistemas / TI"): acceso administrativo completo (crear/editar/desactivar,
///   generar QR, ver credenciales, auditoría, reportes).
/// - Jefe: solo consulta (personas, equipos, servicios, información general) más generación
///   y descarga de reportes. Nunca puede crear/editar/desactivar/administrar nada.
/// Los roles anteriores (Administrador, Tecnico IT, Consulta) se migran automáticamente a estos
/// dos en <see cref="OptivosaITManager.Data.DbInitializer"/>; ver <see cref="LegacyRoleMap"/>.
/// </summary>
public static class Roles
{
    public const string SistemasTI = "Sistemas / TI";
    public const string Jefe = "Jefe";

    public static readonly string[] All = [SistemasTI, Jefe];

    /// <summary>Puede crear/editar/desactivar/administrar (equipos, personas, credenciales, servicios, QR, configuración).</summary>
    public const string PuedeEditar = SistemasTI;

    /// <summary>Puede ver contraseñas de credenciales autorizadas.</summary>
    public const string PuedeVerCredenciales = SistemasTI;

    /// <summary>Nombres de roles previos a la simplificación a dos roles, mapeados a su equivalente nuevo.</summary>
    public static readonly (string LegacyName, string NewName)[] LegacyRoleMap =
    {
        ("Administrador", SistemasTI),
        ("Tecnico IT", SistemasTI),
        ("Consulta", Jefe)
    };

    /// <summary>
    /// Nombre a mostrar en pantalla para un rol. Los valores actuales (SistemasTI, Jefe) ya son
    /// el texto deseado; esta función solo protege contra el caso de un rol heredado que aún no
    /// se haya migrado (ver LegacyRoleMap) para que nunca se muestre un nombre interno crudo.
    /// </summary>
    public static string DisplayName(string roleName)
    {
        foreach (var (legacyName, newName) in LegacyRoleMap)
        {
            if (roleName == legacyName) return newName;
        }
        return roleName;
    }
}
