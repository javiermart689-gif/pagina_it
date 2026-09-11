namespace OptivosaITManager.Security;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string TecnicoIT = "Tecnico IT";
    public const string Consulta = "Consulta";

    public static readonly string[] All = [Administrador, TecnicoIT, Consulta];

    /// <summary>Puede modificar equipos, usuarios, asignaciones y mantenimiento.</summary>
    public const string PuedeEditar = $"{Administrador},{TecnicoIT}";

    /// <summary>Puede ver contraseñas de credenciales autorizadas.</summary>
    public const string PuedeVerCredenciales = $"{Administrador},{TecnicoIT}";

    /// <summary>
    /// Nombre a mostrar en pantalla para un rol. El valor de <see cref="TecnicoIT"/> ("Tecnico IT")
    /// es el identificador real usado en autorización y persistido en AspNetRoles: no se puede
    /// renombrar sin romper las asignaciones de rol ya existentes. Esta función solo traduce
    /// el texto que ve el usuario (regla "IT" -> "TI"), nunca el valor comparado/almacenado.
    /// </summary>
    public static string DisplayName(string roleName) => roleName == TecnicoIT ? "Técnico TI" : roleName;
}
