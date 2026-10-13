using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class ZonasHorariasCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {




            migrationBuilder.AddColumn<string>(
                name: "ZonaHoraria",
                table: "Comercios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "America/Mexico_City");

            migrationBuilder.AddColumn<string>(
                name: "ZonaHoraria",
                table: "citas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "America/Mexico_City");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZonaHoraria",
                table: "Comercios");

            migrationBuilder.DropColumn(
                name: "ZonaHoraria",
                table: "citas");




        }
    }
}
