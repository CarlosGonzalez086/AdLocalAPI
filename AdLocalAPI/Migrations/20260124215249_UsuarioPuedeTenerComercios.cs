using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioPuedeTenerComercios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Ubicacion",
                table: "Comercios",
                type: "geometry(Point,4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography (point, 4326)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Ubicacion",
                table: "Comercios",
                type: "geography (point, 4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geometry(Point,4326)",
                oldNullable: true);
        }
    }
}
