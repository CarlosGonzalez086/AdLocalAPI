using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAgendaCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "citas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    IdProductoServicio = table.Column<long>(type: "bigint", nullable: false),
                    NombrePersona = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NotasCliente = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NombreAtiende = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    MotivoCancelacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_citas_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_citas_ProductosServicios_IdProductoServicio",
                        column: x => x.IdProductoServicio,
                        principalTable: "ProductosServicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_citas_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_citas_IdComercio_FechaInicio_FechaFin",
                table: "citas",
                columns: new[] { "IdComercio", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_citas_IdProductoServicio",
                table: "citas",
                column: "IdProductoServicio");

            migrationBuilder.CreateIndex(
                name: "IX_citas_IdUsuario_FechaInicio",
                table: "citas",
                columns: new[] { "IdUsuario", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_citas_Uuid",
                table: "citas",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "citas");
        }
    }
}
