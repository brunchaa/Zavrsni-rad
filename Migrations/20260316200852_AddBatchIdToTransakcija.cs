using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkladisteRobe.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchIdToTransakcija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "Transakcije",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "Transakcije");
        }
    }
}
