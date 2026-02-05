using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRedeemFieldsToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RedeemMonthFree",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RedeemRewards",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedeemMonthFree",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "RedeemRewards",
                table: "Usuarios");
        }
    }
}
