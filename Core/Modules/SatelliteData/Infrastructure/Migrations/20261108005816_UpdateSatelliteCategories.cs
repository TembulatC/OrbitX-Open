using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Modules.SatelliteData.Domain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSatelliteCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Satellites_Category",
                table: "Satellites");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Satellites");

            migrationBuilder.AddColumn<List<string>>(
                name: "Categories",
                table: "Satellites",
                type: "text[]",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Satellites_Categories",
                table: "Satellites",
                column: "Categories")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Satellites_Categories",
                table: "Satellites");

            migrationBuilder.DropColumn(
                name: "Categories",
                table: "Satellites");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Satellites",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Satellites_Category",
                table: "Satellites",
                column: "Category");
        }
    }
}
