using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class CorregirUuidProductosServicios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Uuid",
                table: "ProductosServicios",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.Sql("UPDATE \"ProductosServicios\" SET \"Uuid\" = gen_random_uuid() WHERE \"Uuid\" = '00000000-0000-0000-0000-000000000000' OR \"Uuid\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosServicios_Uuid",
                table: "ProductosServicios",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductosServicios_Uuid",
                table: "ProductosServicios");

            migrationBuilder.AlterColumn<Guid>(
                name: "Uuid",
                table: "ProductosServicios",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");
        }
    }
}
