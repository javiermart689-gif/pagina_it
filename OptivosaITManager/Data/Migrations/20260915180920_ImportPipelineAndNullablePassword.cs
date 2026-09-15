using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptivosaITManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImportPipelineAndNullablePassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EncryptedPassword",
                table: "Accesses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ImportHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ImportedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    DevicesCreated = table.Column<int>(type: "int", nullable: false),
                    DevicesUpdated = table.Column<int>(type: "int", nullable: false),
                    EmployeesCreated = table.Column<int>(type: "int", nullable: false),
                    EmployeesUpdated = table.Column<int>(type: "int", nullable: false),
                    AssignmentsCreated = table.Column<int>(type: "int", nullable: false),
                    AssignmentsReassigned = table.Column<int>(type: "int", nullable: false),
                    AccessesCreated = table.Column<int>(type: "int", nullable: false),
                    AccessesUpdated = table.Column<int>(type: "int", nullable: false),
                    SkippedRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportHistories_AspNetUsers_ImportedByUserId",
                        column: x => x.ImportedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportHistories_ImportedAt",
                table: "ImportHistories",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportHistories_ImportedByUserId",
                table: "ImportHistories",
                column: "ImportedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportHistories");

            migrationBuilder.AlterColumn<string>(
                name: "EncryptedPassword",
                table: "Accesses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
