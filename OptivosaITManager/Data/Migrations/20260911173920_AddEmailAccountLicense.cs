using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptivosaITManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAccountLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "License",
                table: "EmailAccounts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "License",
                table: "EmailAccounts");
        }
    }
}
