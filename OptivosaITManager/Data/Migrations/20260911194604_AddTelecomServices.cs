using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptivosaITManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelecomServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelecomServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ServiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ProviderContact = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelecomServices_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelecomServices_AccountNumber",
                table: "TelecomServices",
                column: "AccountNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomServices_LocationId",
                table: "TelecomServices",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomServices_PhoneNumber",
                table: "TelecomServices",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomServices_Provider",
                table: "TelecomServices",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomServices_ServiceNumber",
                table: "TelecomServices",
                column: "ServiceNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelecomServices");
        }
    }
}
