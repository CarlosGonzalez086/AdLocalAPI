using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Suscripcions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenovacion",
                table: "Suscripcions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminada",
                table: "Suscripcions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCancelacion",
                table: "Suscripcions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Suscripcions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Monto",
                table: "Suscripcions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "AutoRenovacion",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "Eliminada",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "FechaCancelacion",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "Monto",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "Suscripcions");
        }
    }
}
