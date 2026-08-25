using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTarifaEnvioYCotizaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoEnvio",
                table: "pedidos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CompraMinimaEnvioGratis",
                table: "configuracion_pago_comercio",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoEnvio",
                table: "configuracion_pago_comercio",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoEnvio",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "CompraMinimaEnvioGratis",
                table: "configuracion_pago_comercio");

            migrationBuilder.DropColumn(
                name: "CostoEnvio",
                table: "configuracion_pago_comercio");
        }
    }
}
