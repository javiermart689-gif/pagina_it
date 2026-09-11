using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptivosaITManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceQrToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrToken",
                table: "Devices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_QrToken",
                table: "Devices",
                column: "QrToken",
                unique: true,
                filter: "[QrToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_QrToken",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "QrToken",
                table: "Devices");
        }
    }
}
