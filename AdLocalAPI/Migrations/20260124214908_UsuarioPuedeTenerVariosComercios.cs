using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioPuedeTenerVariosComercios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Comercios_ComercioId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ComercioId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Comercios_IdUsuario",
                table: "Comercios");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "Usuarios",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "UsuarioId",
                table: "Suscripcions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "UsuarioId",
                table: "Publicidades",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "UsuarioId",
                table: "Promociones",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "UsuarioId",
                table: "Eventos",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Point>(
                name: "Ubicacion",
                table: "Comercios",
                type: "geography (point, 4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geometry(Point,4326)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comercios_IdUsuario",
                table: "Comercios",
                column: "IdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Comercios_Usuarios_IdUsuario",
                table: "Comercios",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comercios_Usuarios_IdUsuario",
                table: "Comercios");

            migrationBuilder.DropIndex(
                name: "IX_Comercios_IdUsuario",
                table: "Comercios");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Suscripcions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Publicidades",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Promociones",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Eventos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<Point>(
                name: "Ubicacion",
                table: "Comercios",
                type: "geometry(Point,4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography (point, 4326)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ComercioId",
                table: "Usuarios",
                column: "ComercioId");

            migrationBuilder.CreateIndex(
                name: "IX_Comercios_IdUsuario",
                table: "Comercios",
                column: "IdUsuario",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Comercios_ComercioId",
                table: "Usuarios",
                column: "ComercioId",
                principalTable: "Comercios",
                principalColumn: "Id");
        }
    }
}
