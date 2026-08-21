using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCuentasAdLocalYPagosComisiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cuentas_bancarias_adlocal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Beneficiario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NumeroCuenta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Clabe = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    NumeroTarjeta = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    Instrucciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Principal = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_bancarias_adlocal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pagos_comisiones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    IdCuentaBancariaAdLocal = table.Column<long>(type: "bigint", nullable: false),
                    Periodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ComprobanteUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Estatus = table.Column<int>(type: "integer", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdUsuarioCreacion = table.Column<long>(type: "bigint", nullable: false),
                    IdUsuarioRevision = table.Column<long>(type: "bigint", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_comisiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagos_comisiones_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_comisiones_cuentas_bancarias_adlocal_IdCuentaBancaria~",
                        column: x => x.IdCuentaBancariaAdLocal,
                        principalTable: "cuentas_bancarias_adlocal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_comisiones_detalle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdPagoComision = table.Column<long>(type: "bigint", nullable: false),
                    IdComision = table.Column<long>(type: "bigint", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_comisiones_detalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagos_comisiones_detalle_comisiones_IdComision",
                        column: x => x.IdComision,
                        principalTable: "comisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_comisiones_detalle_pagos_comisiones_IdPagoComision",
                        column: x => x.IdPagoComision,
                        principalTable: "pagos_comisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_adlocal_Principal_Activo",
                table: "cuentas_bancarias_adlocal",
                columns: new[] { "Principal", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_adlocal_Uuid",
                table: "cuentas_bancarias_adlocal",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_comisiones_IdComercio_Estatus",
                table: "pagos_comisiones",
                columns: new[] { "IdComercio", "Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_comisiones_IdCuentaBancariaAdLocal",
                table: "pagos_comisiones",
                column: "IdCuentaBancariaAdLocal");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_comisiones_Uuid",
                table: "pagos_comisiones",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_comisiones_detalle_IdComision",
                table: "pagos_comisiones_detalle",
                column: "IdComision",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_comisiones_detalle_IdPagoComision",
                table: "pagos_comisiones_detalle",
                column: "IdPagoComision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagos_comisiones_detalle");

            migrationBuilder.DropTable(
                name: "pagos_comisiones");

            migrationBuilder.DropTable(
                name: "cuentas_bancarias_adlocal");
        }
    }
}
