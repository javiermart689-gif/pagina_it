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
    Perdido
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
