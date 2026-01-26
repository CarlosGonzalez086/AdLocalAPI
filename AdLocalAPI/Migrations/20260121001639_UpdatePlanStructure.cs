using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlanStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeTexto",
                table: "Plans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ColoresPersonalizados",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxFotos",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxNegocios",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxProductos",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NivelVisibilidad",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PermiteCatalogo",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneAnalytics",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneBadge",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeTexto",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ColoresPersonalizados",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "MaxFotos",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "MaxNegocios",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "MaxProductos",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "NivelVisibilidad",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "PermiteCatalogo",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "TieneAnalytics",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "TieneBadge",
                table: "Plans");
        }
    }
}
