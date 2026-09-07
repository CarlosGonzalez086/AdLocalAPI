using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class StripeWebhooksAndUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StripeWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StripeEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_stripecustomerid",
                table: "Usuarios",
                column: "stripecustomerid",
                unique: true,
                filter: "\"stripecustomerid\" IS NOT NULL AND \"stripecustomerid\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripcions_StripeCheckoutSessionId",
                table: "Suscripcions",
                column: "StripeCheckoutSessionId",
                unique: true,
                filter: "\"StripeCheckoutSessionId\" IS NOT NULL AND \"StripeCheckoutSessionId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripcions_StripeSubscriptionId",
                table: "Suscripcions",
                column: "StripeSubscriptionId",
                unique: true,
                filter: "\"StripeSubscriptionId\" IS NOT NULL AND \"StripeSubscriptionId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_EventType",
                table: "StripeWebhookEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_ReceivedAt",
                table: "StripeWebhookEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_StripeEventId",
                table: "StripeWebhookEvents",
                column: "StripeEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeWebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_stripecustomerid",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Suscripcions_StripeCheckoutSessionId",
                table: "Suscripcions");

            migrationBuilder.DropIndex(
                name: "IX_Suscripcions_StripeSubscriptionId",
                table: "Suscripcions");
        }
    }
}
