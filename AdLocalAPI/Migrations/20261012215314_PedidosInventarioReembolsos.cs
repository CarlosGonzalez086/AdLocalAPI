using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class PedidosInventarioReembolsos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<bool>(name: "RestituirReserva", table: "pedidos", type: "boolean", nullable: true);
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "pedidos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "EstadoPagoAnterior",
                table: "pedido_historial_estados",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoPagoNuevo",
                table: "pedido_historial_estados",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Evento",
                table: "pedido_historial_estados",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Estado");

            migrationBuilder.AddColumn<int>(
                name: "StockDescontado",
                table: "pedido_detalles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockPorRestituir",
                table: "pedido_detalles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockRestituido",
                table: "pedido_detalles",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            migrationBuilder.CreateTable(
                name: "pedido_movimientos_inventario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdPedidoDetalle = table.Column<long>(type: "bigint", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido_movimientos_inventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedido_movimientos_inventario_pedido_detalles_IdPedidoDetal~",
                        column: x => x.IdPedidoDetalle,
                        principalTable: "pedido_detalles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedido_reembolsos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPedido = table.Column<long>(type: "bigint", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Metodo = table.Column<int>(type: "integer", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdUsuarioRegistro = table.Column<long>(type: "bigint", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido_reembolsos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedido_reembolsos_pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pedido_movimientos_inventario_IdPedidoDetalle",
                table: "pedido_movimientos_inventario",
                column: "IdPedidoDetalle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedido_reembolsos_IdPedido",
                table: "pedido_reembolsos",
                column: "IdPedido",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedido_reembolsos_Uuid",
                table: "pedido_reembolsos",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pedido_movimientos_inventario");

            migrationBuilder.DropTable(
                name: "pedido_reembolsos");


            migrationBuilder.DropColumn(name: "RestituirReserva", table: "pedidos");
            migrationBuilder.DropColumn(
                name: "Version",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "EstadoPagoAnterior",
                table: "pedido_historial_estados");

            migrationBuilder.DropColumn(
                name: "EstadoPagoNuevo",
                table: "pedido_historial_estados");

            migrationBuilder.DropColumn(
                name: "Evento",
                table: "pedido_historial_estados");

            migrationBuilder.DropColumn(
                name: "StockDescontado",
                table: "pedido_detalles");

            migrationBuilder.DropColumn(
                name: "StockPorRestituir",
                table: "pedido_detalles");

            migrationBuilder.DropColumn(
                name: "StockRestituido",
                table: "pedido_detalles");


        }
    }
}
