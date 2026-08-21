using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class CorregirRelacionPagosComercio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cuentas_bancarias_comercio_Comercios_ComercioId",
                table: "cuentas_bancarias_comercio");

            migrationBuilder.DropIndex(
                name: "IX_cuentas_bancarias_comercio_ComercioId",
                table: "cuentas_bancarias_comercio");

            migrationBuilder.DropIndex(
                name: "IX_configuracion_pago_comercio_IdComercio",
                table: "configuracion_pago_comercio");

            migrationBuilder.DropColumn(
                name: "ComercioId",
                table: "cuentas_bancarias_comercio");

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_pago_comercio_IdComercio",
                table: "configuracion_pago_comercio",
                column: "IdComercio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_pago_comercio_Uuid",
                table: "configuracion_pago_comercio",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_configuracion_pago_comercio_IdComercio",
                table: "configuracion_pago_comercio");

            migrationBuilder.DropIndex(
                name: "IX_configuracion_pago_comercio_Uuid",
                table: "configuracion_pago_comercio");

            migrationBuilder.AddColumn<long>(
                name: "ComercioId",
                table: "cuentas_bancarias_comercio",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_comercio_ComercioId",
                table: "cuentas_bancarias_comercio",
                column: "ComercioId");

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_pago_comercio_IdComercio",
                table: "configuracion_pago_comercio",
                column: "IdComercio");

            migrationBuilder.AddForeignKey(
                name: "FK_cuentas_bancarias_comercio_Comercios_ComercioId",
                table: "cuentas_bancarias_comercio",
                column: "ComercioId",
                principalTable: "Comercios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
