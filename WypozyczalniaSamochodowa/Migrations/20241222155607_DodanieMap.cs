using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WypozyczalniaSamochodowa.Migrations
{
    /// <inheritdoc />
    public partial class DodanieMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Wynajecie",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Wynajecie",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Wynajecie",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Wynajecie");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Wynajecie");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Wynajecie");
        }
    }
}
