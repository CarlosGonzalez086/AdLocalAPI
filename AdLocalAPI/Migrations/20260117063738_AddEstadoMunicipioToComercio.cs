using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEstadoMunicipioToComercio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoId",
                table: "Comercios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MunicipioId",
                table: "Comercios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Comercios_EstadoId",
                table: "Comercios",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comercios_MunicipioId",
                table: "Comercios",
                column: "MunicipioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comercios_estados_EstadoId",
                table: "Comercios",
                column: "EstadoId",
                principalTable: "estados",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comercios_municipios_MunicipioId",
                table: "Comercios",
                column: "MunicipioId",
                principalTable: "municipios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comercios_estados_EstadoId",
                table: "Comercios");

            migrationBuilder.DropForeignKey(
                name: "FK_Comercios_municipios_MunicipioId",
                table: "Comercios");

            migrationBuilder.DropIndex(
                name: "IX_Comercios_EstadoId",
                table: "Comercios");

            migrationBuilder.DropIndex(
                name: "IX_Comercios_MunicipioId",
                table: "Comercios");

            migrationBuilder.DropColumn(
                name: "EstadoId",
                table: "Comercios");

            migrationBuilder.DropColumn(
                name: "MunicipioId",
                table: "Comercios");
        }

    }
}
