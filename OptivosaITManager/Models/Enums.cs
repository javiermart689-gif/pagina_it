using System.ComponentModel.DataAnnotations;

namespace OptivosaITManager.Models;

public enum DeviceType
{
    Laptop,
    PC,
    Monitor,
    Impresora,
    Telefono,
    Tablet,
    Otro
}

public enum DeviceStatus
{
    Disponible,
    Asignado,
    Mantenimiento,
    Baja,
    Perdido,
    // Agregados para el flujo "Recibir equipo". Añadidos al final para no alterar los
    // valores numéricos ya guardados en la BD para los estados existentes.
    EnRevision,
    EnReparacion,
    EnAlmacen
}

public enum EmployeeStatus
{
    Activo,
    Inactivo
}

public enum CredentialType
{
    Equipo,
    Correo,
    Sistema,
    Aplicacion,
    Servicio,
    Administrador,
    Otro,
    // Agregados para la ficha unificada Persona -> Equipo -> Cuentas. Los valores nuevos
    // se añaden al final para no alterar los valores numéricos ya guardados en la BD.
    Dynamics365,
    Vpn
}

public enum EmailAccountType
{
    Corporativo,
    Compartido,
    Distribucion,
    Otro
}

public enum EmailAccountStatus
{
    Activa,
    Suspendida,
    Baja
}

public enum MaintenanceStatus
{
    Reportado,
    EnRevision,
    EnReparacion,
    Reparado,
    Entregado
}

// Tipos de servicio de telecomunicaciones (catálogo de referencia, no un sistema de tickets).
// Para agregar un nuevo tipo en el futuro basta con añadir un valor AL FINAL de este enum,
// nunca insertarlo en medio ni renumerar los existentes (rompería los valores ya guardados).
public enum TelecomServiceType
{
    [Display(Name = "Internet")]
    Internet,
    [Display(Name = "Telefonía")]
    Telefonia,
    [Display(Name = "Línea telefónica")]
    LineaTelefonica,
    [Display(Name = "Enlace")]
    Enlace,
    [Display(Name = "Datos móviles")]
    DatosMoviles,
    [Display(Name = "Otro")]
    Otro
}

public static class TelecomServiceTypeExtensions
{
    /// <summary>Nombre a mostrar en pantalla (usa el mismo [Display] que Html.GetEnumSelectList).</summary>
    public static string DisplayName(this TelecomServiceType type)
    {
        var member = typeof(TelecomServiceType).GetMember(type.ToString())[0];
        var display = member.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
        return display?.Name ?? type.ToString();
    }
}
