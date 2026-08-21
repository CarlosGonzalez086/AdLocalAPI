using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class DirrecionesFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_direcciones_usuarios_Usuarios_IdUsuario",
                table: "direcciones_usuarios");

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "direcciones_usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaActualizacion",
                table: "direcciones_usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminado",
                table: "direcciones_usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_direcciones_usuarios_Usuarios_IdUsuario",
                table: "direcciones_usuarios",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_direcciones_usuarios_Usuarios_IdUsuario",
                table: "direcciones_usuarios");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "direcciones_usuarios");

            migrationBuilder.DropColumn(
                name: "FechaActualizacion",
                table: "direcciones_usuarios");

            migrationBuilder.DropColumn(
                name: "FechaEliminado",
                table: "direcciones_usuarios");

            migrationBuilder.AddForeignKey(
                name: "FK_direcciones_usuarios_Usuarios_IdUsuario",
                table: "direcciones_usuarios",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
