using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaComercioVisitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comercio_visitas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComercioId = table.Column<long>(type: "bigint", nullable: false),
                    FechaVisita = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ip = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comercio_visitas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comercio_visitas_ComercioId",
                table: "comercio_visitas",
                column: "ComercioId");

            migrationBuilder.CreateIndex(
                name: "IX_comercio_visitas_FechaVisita",
                table: "comercio_visitas",
                column: "FechaVisita");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comercio_visitas");
        }
    }
}
