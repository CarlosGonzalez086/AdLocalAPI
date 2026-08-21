using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class CorregirRelacionPagosComercioFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pedido_detalles_ProductosServicios_IdProductoServicio",
                table: "pedido_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_historial_Usuarios_IdUsuario",
                table: "pedido_historial");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_historial_pedidos_IdPedido",
                table: "pedido_historial");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_Folio",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "IX_pedido_historial_FechaCreacion",
                table: "pedido_historial");

            migrationBuilder.DropIndex(
                name: "IX_pedido_historial_IdPedido",
                table: "pedido_historial");

            migrationBuilder.DropIndex(
                name: "IX_pedido_historial_IdUsuario",
                table: "pedido_historial");

            migrationBuilder.DropIndex(
                name: "IX_pedido_historial_Uuid",
                table: "pedido_historial");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "FechaAceptacion",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "Folio",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "pedido_detalles");

            migrationBuilder.RenameColumn(
                name: "TelefonoRecibe",
                table: "pedidos",
                newName: "TelefonoEntrega");

            migrationBuilder.RenameColumn(
                name: "ReferenciasEntrega",
                table: "pedidos",
                newName: "ObservacionesCliente");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "pedidos",
                newName: "DireccionReferencias");

            migrationBuilder.RenameColumn(
                name: "NombreRecibe",
                table: "pedidos",
                newName: "DireccionMunicipio");

            migrationBuilder.RenameColumn(
                name: "FechaPreparacion",
                table: "pedidos",
                newName: "FechaFinalizacion");

            migrationBuilder.RenameColumn(
                name: "FechaListo",
                table: "pedidos",
                newName: "FechaComprobantePago");

            migrationBuilder.RenameColumn(
                name: "FechaEnvio",
                table: "pedidos",
                newName: "FechaAprobacion");

            migrationBuilder.RenameColumn(
                name: "FechaCancelacion",
                table: "pedidos",
                newName: "FechaActualizacion");

            migrationBuilder.RenameColumn(
                name: "Estatus",
                table: "pedidos",
                newName: "EstadoPago");

            migrationBuilder.RenameColumn(
                name: "CostoEnvio",
                table: "pedidos",
                newName: "MontoComision");

            migrationBuilder.RenameIndex(
                name: "IX_pedidos_Estatus",
                table: "pedidos",
                newName: "IX_pedidos_EstadoPago");

            migrationBuilder.AddColumn<string>(
                name: "Banco",
                table: "pedidos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Beneficiario",
                table: "pedidos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Clabe",
                table: "pedidos",
                type: "character varying(18)",
                maxLength: 18,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClienteEmail",
                table: "pedidos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClienteNombre",
                table: "pedidos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ComercioLogoUrl",
                table: "pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComercioNombre",
                table: "pedidos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionFija",
                table: "pedidos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ComprobantePagoUrl",
                table: "pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionAlias",
                table: "pedidos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionCalle",
                table: "pedidos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionCodigoPostal",
                table: "pedidos",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionColonia",
                table: "pedidos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionEstado",
                table: "pedidos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DireccionLatitud",
                table: "pedidos",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DireccionLongitud",
                table: "pedidos",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionNumeroExterior",
                table: "pedidos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionNumeroInterior",
                table: "pedidos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "pedidos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InstruccionesTransferencia",
                table: "pedidos",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoComercio",
                table: "pedidos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCuenta",
                table: "pedidos",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroPedido",
                table: "pedidos",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroTarjeta",
                table: "pedidos",
                type: "character varying(19)",
                maxLength: 19,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeComision",
                table: "pedidos",
                type: "numeric(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "pedido_detalles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<long>(
                name: "IdProductoServicio",
                table: "pedido_detalles",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "CodigoInterno",
                table: "pedido_detalles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "pedido_detalles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductoUuid",
                table: "pedido_detalles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "pedido_historial_estados",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPedido = table.Column<long>(type: "bigint", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "integer", nullable: true),
                    EstadoNuevo = table.Column<int>(type: "integer", nullable: false),
                    IdUsuarioCambio = table.Column<long>(type: "bigint", nullable: true),
                    Comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido_historial_estados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedido_historial_estados_pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_Estado",
                table: "pedidos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_NumeroPedido",
                table: "pedidos",
                column: "NumeroPedido",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedido_historial_estados_FechaCreacion",
                table: "pedido_historial_estados",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_historial_estados_IdPedido",
                table: "pedido_historial_estados",
                column: "IdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_historial_estados_Uuid",
                table: "pedido_historial_estados",
                column: "Uuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_detalles_ProductosServicios_IdProductoServicio",
                table: "pedido_detalles",
                column: "IdProductoServicio",
                principalTable: "ProductosServicios",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pedido_detalles_ProductosServicios_IdProductoServicio",
                table: "pedido_detalles");

            migrationBuilder.DropTable(
                name: "pedido_historial_estados");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_Estado",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_NumeroPedido",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "Banco",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "Beneficiario",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "Clabe",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "ClienteEmail",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "ClienteNombre",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "ComercioLogoUrl",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "ComercioNombre",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "ComisionFija",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "ComprobantePagoUrl",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionAlias",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionCalle",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionCodigoPostal",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionColonia",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionEstado",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionLatitud",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionLongitud",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionNumeroExterior",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "DireccionNumeroInterior",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "InstruccionesTransferencia",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "MontoComercio",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "NumeroCuenta",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "NumeroPedido",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "NumeroTarjeta",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "PorcentajeComision",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "CodigoInterno",
                table: "pedido_detalles");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "pedido_detalles");

            migrationBuilder.DropColumn(
                name: "ProductoUuid",
                table: "pedido_detalles");

            migrationBuilder.RenameColumn(
                name: "TelefonoEntrega",
                table: "pedidos",
                newName: "TelefonoRecibe");

            migrationBuilder.RenameColumn(
                name: "ObservacionesCliente",
                table: "pedidos",
                newName: "ReferenciasEntrega");

            migrationBuilder.RenameColumn(
                name: "MontoComision",
                table: "pedidos",
                newName: "CostoEnvio");

            migrationBuilder.RenameColumn(
                name: "FechaFinalizacion",
                table: "pedidos",
                newName: "FechaPreparacion");

            migrationBuilder.RenameColumn(
                name: "FechaComprobantePago",
                table: "pedidos",
                newName: "FechaListo");

            migrationBuilder.RenameColumn(
                name: "FechaAprobacion",
                table: "pedidos",
                newName: "FechaEnvio");

            migrationBuilder.RenameColumn(
                name: "FechaActualizacion",
                table: "pedidos",
                newName: "FechaCancelacion");

            migrationBuilder.RenameColumn(
                name: "EstadoPago",
                table: "pedidos",
                newName: "Estatus");

            migrationBuilder.RenameColumn(
                name: "DireccionReferencias",
                table: "pedidos",
                newName: "Observaciones");

            migrationBuilder.RenameColumn(
                name: "DireccionMunicipio",
                table: "pedidos",
                newName: "NombreRecibe");

            migrationBuilder.RenameIndex(
                name: "IX_pedidos_EstadoPago",
                table: "pedidos",
                newName: "IX_pedidos_Estatus");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "pedidos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptacion",
                table: "pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Folio",
                table: "pedidos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "pedido_detalles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<long>(
                name: "IdProductoServicio",
                table: "pedido_detalles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "pedido_detalles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_Folio",
                table: "pedidos",
                column: "Folio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedido_historial_FechaCreacion",
                table: "pedido_historial",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_historial_IdPedido",
                table: "pedido_historial",
                column: "IdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_historial_IdUsuario",
                table: "pedido_historial",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_historial_Uuid",
                table: "pedido_historial",
                column: "Uuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_detalles_ProductosServicios_IdProductoServicio",
                table: "pedido_detalles",
                column: "IdProductoServicio",
                principalTable: "ProductosServicios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_historial_Usuarios_IdUsuario",
                table: "pedido_historial",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_historial_pedidos_IdPedido",
                table: "pedido_historial",
                column: "IdPedido",
                principalTable: "pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
