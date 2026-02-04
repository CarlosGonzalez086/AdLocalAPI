using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixOneToOneUsuarioSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripcions_UsuarioId",
                table: "Suscripcions",
                column: "UsuarioId");
        }
    }
}
