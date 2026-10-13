using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class IntegridadCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Conflicting historical appointments must be reconciled before migration.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("ALTER TABLE citas ADD CONSTRAINT \"EX_citas_comercio_intervalo\" EXCLUDE USING gist (\"IdComercio\" WITH =, tsrange(\"FechaInicio\", \"FechaFin\", '[)') WITH &&) WHERE (\"Estado\" <> 5);");




            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "citas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "citas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "citas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "cita_historial",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCita = table.Column<long>(type: "bigint", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    Evento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EstadoAnterior = table.Column<int>(type: "integer", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "integer", nullable: false),
                    InicioAnterior = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    InicioNuevo = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cita_historial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cita_historial_citas_IdCita",
                        column: x => x.IdCita,
                        principalTable: "citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_citas_IdUsuario_IdempotencyKey",
                table: "citas",
                columns: new[] { "IdUsuario", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_citas_estado",
                table: "citas",
                sql: "\"Estado\" BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_citas_intervalo",
                table: "citas",
                sql: "\"FechaFin\" > \"FechaInicio\"");

            migrationBuilder.CreateIndex(
                name: "IX_cita_historial_IdCita_Fecha",
                table: "cita_historial",
                columns: new[] { "IdCita", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE citas DROP CONSTRAINT \"EX_citas_comercio_intervalo\";");
            migrationBuilder.DropTable(
                name: "cita_historial");

            migrationBuilder.DropIndex(
                name: "IX_citas_IdUsuario_IdempotencyKey",
                table: "citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_citas_estado",
                table: "citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_citas_intervalo",
                table: "citas");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "citas");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "citas");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "citas");




        }
    }
}
