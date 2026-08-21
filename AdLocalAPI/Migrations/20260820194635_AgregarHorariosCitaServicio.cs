using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHorariosCitaServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "horarios_cita_servicio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdProductoServicio = table.Column<long>(type: "bigint", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    IdCita = table.Column<long>(type: "bigint", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarios_cita_servicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_horarios_cita_servicio_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_horarios_cita_servicio_ProductosServicios_IdProductoServicio",
                        column: x => x.IdProductoServicio,
                        principalTable: "ProductosServicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_horarios_cita_servicio_citas_IdCita",
                        column: x => x.IdCita,
                        principalTable: "citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_horarios_cita_servicio_IdCita",
                table: "horarios_cita_servicio",
                column: "IdCita");

            migrationBuilder.CreateIndex(
                name: "IX_horarios_cita_servicio_IdComercio_Fecha_Disponible",
                table: "horarios_cita_servicio",
                columns: new[] { "IdComercio", "Fecha", "Disponible" });

            migrationBuilder.CreateIndex(
                name: "IX_horarios_cita_servicio_IdProductoServicio_Fecha_HoraInicio",
                table: "horarios_cita_servicio",
                columns: new[] { "IdProductoServicio", "Fecha", "HoraInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_horarios_cita_servicio_Uuid",
                table: "horarios_cita_servicio",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "horarios_cita_servicio");
        }
    }
}
