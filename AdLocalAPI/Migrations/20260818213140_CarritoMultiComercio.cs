using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class CarritoMultiComercio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_carritos_Comercios_IdComercio",
                table: "carritos");

            migrationBuilder.DropForeignKey(
                name: "FK_carritos_Usuarios_IdUsuario",
                table: "carritos");

            migrationBuilder.DropIndex(
                name: "IX_carritos_IdComercio",
                table: "carritos");

            migrationBuilder.DropIndex(
                name: "IX_carritos_IdUsuario",
                table: "carritos");

            migrationBuilder.DropIndex(
                name: "IX_carrito_detalles_IdCarrito",
                table: "carrito_detalles");

            migrationBuilder.DropColumn(
                name: "IdComercio",
                table: "carritos");

            migrationBuilder.AddForeignKey(
                name: "FK_carritos_Usuarios_IdUsuario",
                table: "carritos",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_carritos_Usuarios_IdUsuario",
                table: "carritos");

            migrationBuilder.AddColumn<long>(
                name: "IdComercio",
                table: "carritos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_carritos_IdComercio",
                table: "carritos",
                column: "IdComercio");

            migrationBuilder.CreateIndex(
                name: "IX_carritos_IdUsuario",
                table: "carritos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_carrito_detalles_IdCarrito",
                table: "carrito_detalles",
                column: "IdCarrito");

            migrationBuilder.AddForeignKey(
                name: "FK_carritos_Comercios_IdComercio",
                table: "carritos",
                column: "IdComercio",
                principalTable: "Comercios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_carritos_Usuarios_IdUsuario",
                table: "carritos",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
