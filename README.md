# Optivosa IT Manager

Sistema web interno para el departamento de IT de Optivosa. Centraliza la información que
actualmente se administra en hojas de Excel: equipos de cómputo, usuarios/empleados, cuentas
de correo, credenciales, asignaciones/reasignaciones de equipos, mantenimiento y auditoría.

## Stack tecnológico

- **.NET 8 / ASP.NET Core MVC** (Razor Views, no SPA)
- **Entity Framework Core 8** (Code-First, migraciones)
- **ASP.NET Core Identity** (autenticación, roles)
- **Microsoft SQL Server**
- **Bootstrap 5** (servido localmente, sin dependencias de CDN en producción)
- **ClosedXML** para importación/lectura de archivos Excel (.xlsx)

## Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019+ (Developer, Standard, Enterprise o Express) o Azure SQL
- Herramienta `dotnet-ef` (`dotnet tool install --global dotnet-ef --version 8.0.11`)

## Estructura del proyecto

```
OptivosaITManager/
├── Controllers/          MVC + Controllers/Api (API REST)
├── Data/                 ApplicationDbContext, DbInitializer, Migrations
├── Models/                Entidades de dominio y enums
├── ViewModels/            ViewModels y DTOs de la API
├── Services/              AuditService, SearchService, ExcelImportService
├── Security/              Roles, cifrado AES de credenciales
├── Views/                 Razor Views organizadas por módulo
├── wwwroot/               CSS, JS, librerías estáticas (Bootstrap, jQuery)
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

## 1. Configuración de secretos (obligatorio antes de ejecutar)

La aplicación necesita tres valores que **nunca deben estar en el repositorio**:

| Configuración | Descripción |
|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server |
| `Encryption:Key` | Clave AES-256 en Base64 para cifrar contraseñas de Credenciales/Correos |
| `InitialAdmin:Password` | Contraseña del usuario administrador que se crea al primer arranque |

### En desarrollo (User Secrets)

```bash
cd OptivosaITManager
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=OptivosaITManagerDev;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"

# Generar una clave AES-256 aleatoria en Base64:
# Linux/macOS: openssl rand -base64 32
# Windows PowerShell: [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
dotnet user-secrets set "Encryption:Key" "PEGAR_AQUI_LA_CLAVE_BASE64"

dotnet user-secrets set "InitialAdmin:Password" "UnaContraseñaSegura#2026"
```

### En producción (variables de entorno)

ASP.NET Core mapea automáticamente variables de entorno con `__` como separador de sección:

```
ConnectionStrings__DefaultConnection=Server=SQLSERVER01;Database=OptivosaITManager;User Id=optivosa_app;Password=...;TrustServerCertificate=True;
Encryption__Key=<clave base64 de 32 bytes>
InitialAdmin__Password=<contraseña temporal, cámbiela después del primer login>
```

En IIS, estas variables se configuran en el Application Pool o en `web.config` (ver sección de
publicación más abajo) — **nunca** en `appsettings.Production.json` versionado.

> **Importante:** si se pierde o cambia `Encryption:Key`, todas las contraseñas ya cifradas en
> la base de datos dejan de poder desencriptarse. Consérvela en un gestor de secretos (Azure Key
> Vault, un vault interno, etc.) y no solo en la configuración del servidor.

## 2. Migraciones y base de datos

Las migraciones **no se aplican automáticamente** al iniciar la aplicación (por diseño, para
evitar cambios de esquema no controlados en producción). Se aplican manualmente:

```bash
cd OptivosaITManager
dotnet ef database update
```

Esto crea la base de datos (si no existe) y todas las tablas: Identity (usuarios/roles) y las
entidades de negocio (Devices, Employees, Credentials, EmailAccounts, DeviceAssignments,
Maintenance, AuditLogs, Departments, Locations).

Si la aplicación detecta migraciones pendientes al arrancar, lo registra como advertencia en el
log pero no bloquea el arranque (para no impedir troubleshooting).

Para crear una nueva migración tras modificar un modelo:

```bash
dotnet ef migrations add NombreDescriptivo
dotnet ef database update
```

## 3. Ejecutar el proyecto en desarrollo

```bash
cd OptivosaITManager
dotnet run
```

La aplicación abre en `https://localhost:5001` (o el puerto indicado en consola). Al iniciar,
si la base de datos ya tiene las migraciones aplicadas, se crean automáticamente:

- Los 3 roles: `Administrador`, `Tecnico IT`, `Consulta`
- El usuario administrador inicial (correo configurable en `InitialAdmin:Email`,
  por defecto `admin@optivosa.local`, con la contraseña definida en `InitialAdmin:Password`)
- Departamentos y ubicaciones básicos (editables luego desde **Configuración**)

**Inicie sesión con ese usuario administrador la primera vez** y cree las cuentas reales del
equipo de IT desde **Configuración → Usuarios y roles**, cambiando o deshabilitando el usuario
inicial si lo desea.

## 4. Roles y permisos

| Rol | Permisos |
|---|---|
| **Administrador** | Acceso total: CRUD de equipos, usuarios, credenciales, correos, mantenimiento, asignaciones, configuración, auditoría y gestión de usuarios del sistema. |
| **Tecnico IT** | Consultar equipos/usuarios, asignar/reasignar equipos, registrar mantenimiento, consultar (mostrar/copiar) credenciales autorizadas. No puede crear/editar credenciales, correos, ni gestionar usuarios del sistema. |
| **Consulta** | Solo lectura de equipos, usuarios y correos. Nunca ve contraseñas (ni botón de "Mostrar"/"Copiar"). |

## 5. Importación de Excel

En **Equipos → Importar Excel** se puede cargar un archivo `.xlsx` con las columnas:

- Obligatorias: `InventoryNumber`, `DeviceType`, `Status`
- Opcionales: `Brand`, `Model`, `SerialNumber`, `ComputerName`, `OperatingSystem`, `Notes`

El sistema valida cada fila (formato, duplicados contra la BD y dentro del archivo, registros
incompletos) y muestra un resumen (válidos / duplicados / errores / incompletos) **antes** de
guardar nada. Solo se importan los registros marcados como válidos, y únicamente tras
confirmación explícita del usuario.

## 6. API REST

Endpoints disponibles bajo `/api`, protegidos con la misma autenticación por cookies de Identity
y autorizados por rol:

```
GET  /api/devices
GET  /api/devices/{id}
POST /api/devices                 (Administrador / Tecnico IT)
PUT  /api/devices/{id}             (Administrador / Tecnico IT)

GET  /api/employees
GET  /api/employees/{id}

POST /api/deviceassignments         (Administrador / Tecnico IT)
POST /api/deviceassignments/reassign (Administrador / Tecnico IT)

GET  /api/maintenance

GET  /api/audit                     (Administrador)
```

Los DTOs de la API nunca incluyen contraseñas ni valores cifrados.

## 7. Publicación en Windows Server + IIS

### 7.1 Requisitos en el servidor

1. Instalar el **ASP.NET Core Hosting Bundle** (incluye el runtime y el módulo `ANCM` para IIS):
   https://dotnet.microsoft.com/download/dotnet/8.0 → "Hosting Bundle".
2. Reiniciar IIS tras la instalación (`net stop was /y && net start w3svc`) o reiniciar el
   servidor.
3. Tener acceso a una instancia de SQL Server (local o remota) y un usuario/rol con permisos
   sobre la base de datos `OptivosaITManager`.

### 7.2 Publicar la aplicación

Desde la máquina de desarrollo:

```bash
cd OptivosaITManager
dotnet publish -c Release -o C:\publish\OptivosaITManager
```

Copie el contenido de `C:\publish\OptivosaITManager` a la carpeta del sitio en el servidor
(por ejemplo `C:\inetpub\wwwroot\OptivosaITManager`).

### 7.3 Crear el sitio en IIS

1. Abra **Administrador de Internet Information Services (IIS)**.
2. Cree un **Application Pool** nuevo:
   - .NET CLR version: **No Managed Code** (ASP.NET Core se ejecuta fuera del CLR de IIS)
   - Managed pipeline mode: Integrated
3. Cree un nuevo **Sitio web**, apuntando a la carpeta publicada, y asígnele el Application Pool
   creado.
4. Configure el **binding HTTPS** con un certificado válido (interno o de una CA corporativa).
   Fuerce redirección HTTP→HTTPS (la aplicación ya usa `UseHttpsRedirection`/`UseHsts`).

### 7.4 Variables de entorno / connection string en producción

En el Application Pool (o en `web.config` generado por `dotnet publish`, dentro del elemento
`<aspNetCore>`), configure las variables de entorno necesarias:

```xml
<aspNetCore processPath="dotnet" arguments=".\OptivosaITManager.dll" stdoutLogEnabled="false"
            hostingModel="InProcess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__DefaultConnection"
                          value="Server=SQLSERVER01;Database=OptivosaITManager;User Id=optivosa_app;Password=...;TrustServerCertificate=True;" />
    <environmentVariable name="Encryption__Key" value="<clave base64>" />
    <environmentVariable name="InitialAdmin__Password" value="<contraseña temporal>" />
  </environmentVariables>
</aspNetCore>
```

> Alternativamente, defina estas variables a nivel de Application Pool (`Configuration Editor` →
> `system.applicationHost/applicationPools`) para no dejarlas visibles en `web.config` en texto
> plano dentro de la carpeta publicada.

### 7.5 Aplicar migraciones en el servidor

Antes del primer arranque en producción, ejecute (desde una máquina con acceso a la base de
datos y al SDK de .NET, o publicando `dotnet-ef` junto con el sitio):

```bash
dotnet ef database update --connection "Server=SQLSERVER01;Database=OptivosaITManager;..."
```

### 7.6 Verificación

Acceda a `https://<su-dominio-o-ip>/` — debe redirigir a `/Account/Login`. Inicie sesión con el
usuario administrador inicial y cambie/cree los usuarios reales del equipo.

## 8. Consideraciones de seguridad

- Las contraseñas de `Credential` y `EmailAccount` se cifran con **AES-256-GCM**, con un nonce
  aleatorio por registro; nunca se almacenan en texto plano ni se hashéan (deben poder
  recuperarse para usuarios autorizados).
- Las contraseñas nunca aparecen en listados, respuestas JSON por defecto, logs de auditoría, ni
  mensajes de error; solo se desencriptan en memoria al invocar explícitamente "Mostrar
  contraseña" o "Copiar", y ambas acciones quedan registradas en `AuditLogs`.
- Autenticación vía ASP.NET Core Identity (cookies), con roles y `[Authorize(Roles = ...)]` en
  cada controlador/acción sensible.
- Antiforgery tokens habilitados en todos los formularios y en las llamadas AJAX de
  mostrar/copiar contraseña (`RequestVerificationToken`).
- Protección contra SQL Injection: todo el acceso a datos pasa por EF Core con consultas
  parametrizadas; no hay SQL concatenado a mano en ningún módulo.
- Validación de modelos (`DataAnnotations`) en cada formulario y en los DTOs de la API.
- En producción, las excepciones no se muestran al usuario (`UseExceptionHandler`), y las
  advertencias/errores no incluyen datos sensibles.
- `.gitignore` excluye `appsettings.Production.json`, cualquier `*.local.json`, certificados y
  archivos de secretos — las credenciales reales solo existen en el servidor o en User Secrets
  locales, nunca en el repositorio.

## 9. Comandos útiles

```bash
# Compilar
dotnet build

# Ejecutar en desarrollo
dotnet run

# Crear una migración
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update

# Publicar para IIS
dotnet publish -c Release -o ./publish
```
