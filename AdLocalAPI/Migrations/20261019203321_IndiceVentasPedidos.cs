using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class IndiceVentasPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_pedidos_EstadoPago_FechaCreacion_IdComercio",
                table: "pedidos",
                columns: new[] { "EstadoPago", "FechaCreacion", "IdComercio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pedidos_EstadoPago_FechaCreacion_IdComercio",
                table: "pedidos");
        }
    }
}
