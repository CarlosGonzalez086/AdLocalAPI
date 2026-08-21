using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class PagosComercios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NumeroCuenta",
                table: "cuentas_bancarias_comercio",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ComercioId",
                table: "cuentas_bancarias_comercio",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "configuracion_pago_comercio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    AceptaEfectivo = table.Column<bool>(type: "boolean", nullable: false),
                    AceptaTransferencia = table.Column<bool>(type: "boolean", nullable: false),
                    InstruccionesTransferencia = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_pago_comercio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracion_pago_comercio_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_comercio_ComercioId",
                table: "cuentas_bancarias_comercio",
                column: "ComercioId");

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_pago_comercio_IdComercio",
                table: "configuracion_pago_comercio",
                column: "IdComercio");

            migrationBuilder.AddForeignKey(
                name: "FK_cuentas_bancarias_comercio_Comercios_ComercioId",
                table: "cuentas_bancarias_comercio",
                column: "ComercioId",
                principalTable: "Comercios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cuentas_bancarias_comercio_Comercios_ComercioId",
                table: "cuentas_bancarias_comercio");

            migrationBuilder.DropTable(
                name: "configuracion_pago_comercio");

            migrationBuilder.DropIndex(
                name: "IX_cuentas_bancarias_comercio_ComercioId",
                table: "cuentas_bancarias_comercio");

            migrationBuilder.DropColumn(
                name: "ComercioId",
                table: "cuentas_bancarias_comercio");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCuenta",
                table: "cuentas_bancarias_comercio",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25,
                oldNullable: true);
        }
    }
}
