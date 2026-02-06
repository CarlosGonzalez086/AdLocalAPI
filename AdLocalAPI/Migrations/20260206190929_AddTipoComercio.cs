using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoComercio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TipoComercioId",
                table: "Comercios",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TipoComercio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoComercio", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comercios_TipoComercioId",
                table: "Comercios",
                column: "TipoComercioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comercios_TipoComercio_TipoComercioId",
                table: "Comercios",
                column: "TipoComercioId",
                principalTable: "TipoComercio",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comercios_TipoComercio_TipoComercioId",
                table: "Comercios");

            migrationBuilder.DropTable(
                name: "TipoComercio");

            migrationBuilder.DropIndex(
                name: "IX_Comercios_TipoComercioId",
                table: "Comercios");

            migrationBuilder.DropColumn(
                name: "TipoComercioId",
                table: "Comercios");
        }
    }
}
