using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSuscripcionForStripePeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "FechaFin",
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

            migrationBuilder.RenameColumn(
                name: "StripeSessionId",
                table: "Suscripcions",
                newName: "StripeCheckoutSessionId");

            migrationBuilder.RenameColumn(
                name: "FechaInicio",
                table: "Suscripcions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "FechaCancelacion",
                table: "Suscripcions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Eliminada",
                table: "Suscripcions",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "AutoRenovacion",
                table: "Suscripcions",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Activa",
                table: "Suscripcions",
                newName: "AutoRenew");

            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripePriceId",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledAt",
                table: "Suscripcions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodEnd",
                table: "Suscripcions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodStart",
                table: "Suscripcions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Suscripcions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "Plans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanceledAt",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEnd",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodStart",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Suscripcions");

            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "Plans");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Suscripcions",
                newName: "FechaCancelacion");

            migrationBuilder.RenameColumn(
                name: "StripeCheckoutSessionId",
                table: "Suscripcions",
                newName: "StripeSessionId");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Suscripcions",
                newName: "Eliminada");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Suscripcions",
                newName: "AutoRenovacion");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Suscripcions",
                newName: "FechaInicio");

            migrationBuilder.RenameColumn(
                name: "AutoRenew",
                table: "Suscripcions",
                newName: "Activa");

            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "Suscripcions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StripePriceId",
                table: "Suscripcions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "Suscripcions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Suscripcions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Suscripcions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFin",
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
        }
    }
}
