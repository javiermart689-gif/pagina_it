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
}
