using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductosServiciosTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.CreateTable(
                name: "ProductosServicios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_comercio = table.Column<long>(type: "bigint", nullable: false),
                    id_usuario = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    precio = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    stock = table.Column<int>(type: "integer", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_eliminado = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo_interno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosServicios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductosServicios_activo",
                table: "ProductosServicios",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosServicios_eliminado",
                table: "ProductosServicios",
                column: "eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosServicios_id_comercio",
                table: "ProductosServicios",
                column: "id_comercio");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosServicios_id_usuario",
                table: "ProductosServicios",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductosServicios");

        }
    }
}
