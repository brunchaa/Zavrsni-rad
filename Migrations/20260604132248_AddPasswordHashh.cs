using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkladisteRobe.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Korisnici");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Korisnici");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
