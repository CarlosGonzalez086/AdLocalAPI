using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixUsuarioSuscripcionesRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comercios_TipoComercio_TipoComercioId",
                table: "Comercios");

            migrationBuilder.DropIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comercios_TipoComercio_TipoComercioId",
                table: "Comercios",
                column: "TipoComercioId",
                principalTable: "TipoComercio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comercios_TipoComercio_TipoComercioId",
                table: "Comercios");

            migrationBuilder.DropIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comercios_TipoComercio_TipoComercioId",
                table: "Comercios",
                column: "TipoComercioId",
                principalTable: "TipoComercio",
                principalColumn: "Id");
        }
    }
}
