using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUsosCodigoReferidoConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsosCodigoReferido",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioReferidorId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioReferidoId = table.Column<long>(type: "bigint", nullable: false),
                    CodigoReferido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaUso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsosCodigoReferido", x => x.Id);
                    table.CheckConstraint("CK_NoAutoReferido", "\"UsuarioReferidorId\" <> \"UsuarioReferidoId\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsosCodigoReferido_UsuarioReferidoId",
                table: "UsosCodigoReferido",
                column: "UsuarioReferidoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsosCodigoReferido");
        }
    }
}
