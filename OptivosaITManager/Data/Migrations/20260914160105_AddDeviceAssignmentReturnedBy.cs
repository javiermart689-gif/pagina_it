using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptivosaITManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceAssignmentReturnedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnedByUserId",
                table: "DeviceAssignments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnedByUserId",
                table: "DeviceAssignments");
        }
    }
}
