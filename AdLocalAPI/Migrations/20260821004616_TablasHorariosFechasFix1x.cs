using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class TablasHorariosFechasFix1x : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cotizaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    IdProductoServicio = table.Column<long>(type: "bigint", nullable: false),
                    Solicitud = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Respuesta = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PrecioPropuesto = table.Column<decimal>(type: "numeric", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cotizaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cotizaciones_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cotizaciones_ProductosServicios_IdProductoServicio",
                        column: x => x.IdProductoServicio,
                        principalTable: "ProductosServicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cotizaciones_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cotizaciones_IdComercio",
                table: "cotizaciones",
                column: "IdComercio");

            migrationBuilder.CreateIndex(
                name: "IX_cotizaciones_IdProductoServicio",
                table: "cotizaciones",
                column: "IdProductoServicio");

            migrationBuilder.CreateIndex(
                name: "IX_cotizaciones_IdUsuario_FechaCreacion",
                table: "cotizaciones",
                columns: new[] { "IdUsuario", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_cotizaciones_Uuid",
                table: "cotizaciones",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cotizaciones");
        }
    }
}
