using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class CheckoutIdempotenciaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_stripecustomerid",
                table: "Usuarios");

            migrationBuilder.CreateTable(
                name: "checkout_idempotencias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdempotencyKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCompletado = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_idempotencias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_stripecustomerid",
                table: "Usuarios",
                column: "stripecustomerid",
                unique: true,
                filter: "\"stripecustomerid\" IS NOT NULL AND \"stripecustomerid\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_idempotencias_FechaCreacion",
                table: "checkout_idempotencias",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_idempotencias_IdUsuario_IdempotencyKey",
                table: "checkout_idempotencias",
                columns: new[] { "IdUsuario", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_idempotencias");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_stripecustomerid",
                table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_stripecustomerid",
                table: "Usuarios",
                column: "stripecustomerid",
                unique: true,
                filter: "\"StripeCustomerId\" IS NOT NULL AND \"StripeCustomerId\" <> ''");
        }
    }
}
