using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptivosaITManager.Data.Migrations
{
    /// <inheritdoc />
    // Unifica los antiguos módulos Credenciales (tabla Credentials) y Cuentas de correo (tabla
    // EmailAccounts) en un único módulo Accesos (tabla Accesses). IMPORTANTE: a diferencia de lo
    // que EF Core scaffoldeó automáticamente (que hacía DROP TABLE antes de crear la tabla
    // nueva, perdiendo todos los datos), este Up() primero crea "Accesses", copia los datos
    // existentes de ambas tablas origen con las conversiones de tipo necesarias, y solo entonces
    // elimina las tablas antiguas. No se preservan los Id numéricos originales (Credentials y
    // EmailAccounts tenían cada una su propia secuencia de identidad, y no pueden coexistir sin
    // colisionar en una sola tabla); el histórico de AuditLogs conserva la descripción en texto
    // de cada acción, así que no se pierde información, aunque el Id numérico ahí registrado ya
    // no corresponda a una fila existente.
    public partial class UnifyCredentialsAndEmailAccountsIntoAccesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    ServiceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    License = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accesses_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accesses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accesses_DeviceId",
                table: "Accesses",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Accesses_EmployeeId",
                table: "Accesses",
                column: "EmployeeId");

            // --- Copiar Credentials -> Accesses ---
            // CredentialType (0 Equipo,1 Correo,2 Sistema,3 Aplicacion,4 Servicio,5 Administrador,
            //                 6 Otro,7 Dynamics365,8 Vpn)
            //   -> AccessType (0 Correo,1 Equipo,2 Sistema,3 Aplicacion,4 Vpn,5 Servidor,
            //                  6 Administrativa,7 Otro)
            // Si el Credential estaba vinculado solo a un EmailAccount (sin EmployeeId propio), se
            // hereda el EmployeeId de esa cuenta de correo para no perder la relación con la persona.
            migrationBuilder.Sql(@"
                INSERT INTO Accesses (Name, Type, Username, EncryptedPassword, EmployeeId, DeviceId, ServiceName, License, Status, Notes, CreatedAt, UpdatedAt)
                SELECT
                    c.Name,
                    CASE c.CredentialType
                        WHEN 0 THEN 1
                        WHEN 1 THEN 0
                        WHEN 2 THEN 2
                        WHEN 3 THEN 3
                        WHEN 4 THEN 5
                        WHEN 5 THEN 6
                        WHEN 6 THEN 7
                        WHEN 7 THEN 2
                        WHEN 8 THEN 4
                        ELSE 7
                    END,
                    c.Username,
                    c.EncryptedPassword,
                    COALESCE(c.EmployeeId, ea.EmployeeId),
                    c.DeviceId,
                    NULL,
                    NULL,
                    CASE WHEN c.IsActive = 1 THEN 0 ELSE 2 END,
                    c.Notes,
                    c.CreatedAt,
                    c.UpdatedAt
                FROM Credentials c
                LEFT JOIN EmailAccounts ea ON c.EmailAccountId = ea.Id;
            ");

            // --- Copiar EmailAccounts -> Accesses (todas quedan Type = 0, Correo) ---
            // EmailAccountStatus (0 Activa,1 Suspendida,2 Baja) coincide 1:1 con AccessStatus
            // (0 Activo,1 Suspendido,2 Baja): no requiere conversión.
            migrationBuilder.Sql(@"
                INSERT INTO Accesses (Name, Type, Username, EncryptedPassword, EmployeeId, DeviceId, ServiceName, License, Status, Notes, CreatedAt, UpdatedAt)
                SELECT
                    CASE WHEN e.Id IS NOT NULL THEN e.FirstName + ' ' + e.LastName + ' - ' + COALESCE(ea.License, 'Correo') ELSE ea.Email END,
                    0,
                    ea.Username,
                    ea.EncryptedPassword,
                    ea.EmployeeId,
                    NULL,
                    NULL,
                    ea.License,
                    ea.Status,
                    ea.Notes,
                    ea.CreatedAt,
                    ea.UpdatedAt
                FROM EmailAccounts ea
                LEFT JOIN Employees e ON ea.EmployeeId = e.Id;
            ");

            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "EmailAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accesses");

            migrationBuilder.CreateTable(
                name: "EmailAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    License = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAccounts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    EmailAccountId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CredentialType = table.Column<int>(type: "int", nullable: false),
                    EncryptedPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Credentials_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Credentials_EmailAccounts_EmailAccountId",
                        column: x => x.EmailAccountId,
                        principalTable: "EmailAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Credentials_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_DeviceId",
                table: "Credentials",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_EmailAccountId",
                table: "Credentials",
                column: "EmailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_EmployeeId",
                table: "Credentials",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_Email",
                table: "EmailAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_EmployeeId",
                table: "EmailAccounts",
                column: "EmployeeId");
        }
    }
}
