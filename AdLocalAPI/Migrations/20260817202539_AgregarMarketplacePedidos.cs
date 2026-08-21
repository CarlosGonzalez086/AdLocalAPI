using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AdLocalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMarketplacePedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carritos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carritos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carritos_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_carritos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comisiones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    TipoOperacion = table.Column<int>(type: "integer", nullable: false),
                    IdReferencia = table.Column<long>(type: "bigint", nullable: false),
                    MontoOperacion = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PorcentajeComision = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MontoComision = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estatus = table.Column<int>(type: "integer", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comisiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comisiones_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_comercio_pedidos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    AceptaPedidos = table.Column<bool>(type: "boolean", nullable: false),
                    AceptandoPedidosAhora = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteEfectivo = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteTransferencia = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteRecoger = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteDomicilio = table.Column<bool>(type: "boolean", nullable: false),
                    PedidoMinimo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TiempoPreparacionMinutos = table.Column<int>(type: "integer", nullable: false),
                    CostoEnvio = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CompraMinimaEnvioGratis = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MensajePedidos = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_comercio_pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracion_comercio_pedidos_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_comisiones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoOperacion = table.Column<int>(type: "integer", nullable: false),
                    PorcentajeComision = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ComisionMinima = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ComisionMaxima = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_comisiones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_bancarias_comercio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    Banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Beneficiario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NumeroCuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Clabe = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    NumeroTarjeta = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    Principal = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_bancarias_comercio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cuentas_bancarias_comercio_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "direcciones_usuarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    Alias = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Calle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NumeroExterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NumeroInterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Colonia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IdEstado = table.Column<int>(type: "integer", nullable: false),
                    IdMunicipio = table.Column<int>(type: "integer", nullable: false),
                    Latitud = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    Longitud = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    Referencias = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EsPredeterminada = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direcciones_usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_direcciones_usuarios_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_direcciones_usuarios_estados_IdEstado",
                        column: x => x.IdEstado,
                        principalTable: "estados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_direcciones_usuarios_municipios_IdMunicipio",
                        column: x => x.IdMunicipio,
                        principalTable: "municipios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TipoNotificacion = table.Column<int>(type: "integer", nullable: false),
                    IdReferencia = table.Column<long>(type: "bigint", nullable: true),
                    TipoReferencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Leida = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaLectura = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notificaciones_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "carrito_detalles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdCarrito = table.Column<long>(type: "bigint", nullable: false),
                    IdProductoServicio = table.Column<long>(type: "bigint", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrito_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carrito_detalles_ProductosServicios_IdProductoServicio",
                        column: x => x.IdProductoServicio,
                        principalTable: "ProductosServicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carrito_detalles_carritos_IdCarrito",
                        column: x => x.IdCarrito,
                        principalTable: "carritos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pedidos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Folio = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IdComercio = table.Column<long>(type: "bigint", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CostoEnvio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MetodoPago = table.Column<int>(type: "integer", nullable: false),
                    TipoEntrega = table.Column<int>(type: "integer", nullable: false),
                    Estatus = table.Column<int>(type: "integer", nullable: false),
                    IdDireccionUsuario = table.Column<long>(type: "bigint", nullable: true),
                    NombreRecibe = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TelefonoRecibe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReferenciasEntrega = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaAceptacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaPreparacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaListo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedidos_Comercios_IdComercio",
                        column: x => x.IdComercio,
                        principalTable: "Comercios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_direcciones_usuarios_IdDireccionUsuario",
                        column: x => x.IdDireccionUsuario,
                        principalTable: "direcciones_usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "comprobantes_pago",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPedido = table.Column<long>(type: "bigint", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    ArchivoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estatus = table.Column<int>(type: "integer", nullable: false),
                    IdUsuarioValidacion = table.Column<long>(type: "bigint", nullable: true),
                    Comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaValidacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comprobantes_pago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comprobantes_pago_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_pago_Usuarios_IdUsuarioValidacion",
                        column: x => x.IdUsuarioValidacion,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_comprobantes_pago_pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pedido_detalles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPedido = table.Column<long>(type: "bigint", nullable: false),
                    IdProductoServicio = table.Column<long>(type: "bigint", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedido_detalles_ProductosServicios_IdProductoServicio",
                        column: x => x.IdProductoServicio,
                        principalTable: "ProductosServicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedido_detalles_pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pedido_historial",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPedido = table.Column<long>(type: "bigint", nullable: false),
                    EstatusAnterior = table.Column<int>(type: "integer", nullable: true),
                    EstatusNuevo = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: true),
                    Comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido_historial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedido_historial_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pedido_historial_pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carrito_detalles_IdCarrito",
                table: "carrito_detalles",
                column: "IdCarrito");

            migrationBuilder.CreateIndex(
                name: "IX_carrito_detalles_IdCarrito_IdProductoServicio",
                table: "carrito_detalles",
                columns: new[] { "IdCarrito", "IdProductoServicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carrito_detalles_IdProductoServicio",
                table: "carrito_detalles",
                column: "IdProductoServicio");

            migrationBuilder.CreateIndex(
                name: "IX_carrito_detalles_Uuid",
                table: "carrito_detalles",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carritos_IdComercio",
                table: "carritos",
                column: "IdComercio");

            migrationBuilder.CreateIndex(
                name: "IX_carritos_IdUsuario",
                table: "carritos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_carritos_IdUsuario_Activo",
                table: "carritos",
                columns: new[] { "IdUsuario", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_carritos_Uuid",
                table: "carritos",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comisiones_Estatus",
                table: "comisiones",
                column: "Estatus");

            migrationBuilder.CreateIndex(
                name: "IX_comisiones_IdComercio",
                table: "comisiones",
                column: "IdComercio");

            migrationBuilder.CreateIndex(
                name: "IX_comisiones_TipoOperacion_IdReferencia",
                table: "comisiones",
                columns: new[] { "TipoOperacion", "IdReferencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comisiones_Uuid",
                table: "comisiones",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_pago_Estatus",
                table: "comprobantes_pago",
                column: "Estatus");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_pago_IdPedido",
                table: "comprobantes_pago",
                column: "IdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_pago_IdUsuario",
                table: "comprobantes_pago",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_pago_IdUsuarioValidacion",
                table: "comprobantes_pago",
                column: "IdUsuarioValidacion");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_pago_Uuid",
                table: "comprobantes_pago",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_comercio_pedidos_IdComercio",
                table: "configuracion_comercio_pedidos",
                column: "IdComercio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_comercio_pedidos_Uuid",
                table: "configuracion_comercio_pedidos",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_comisiones_TipoOperacion_Activo",
                table: "configuracion_comisiones",
                columns: new[] { "TipoOperacion", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_comisiones_Uuid",
                table: "configuracion_comisiones",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_comercio_IdComercio",
                table: "cuentas_bancarias_comercio",
                column: "IdComercio");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_comercio_IdComercio_Principal",
                table: "cuentas_bancarias_comercio",
                columns: new[] { "IdComercio", "Principal" });

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_bancarias_comercio_Uuid",
                table: "cuentas_bancarias_comercio",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_direcciones_usuarios_IdEstado",
                table: "direcciones_usuarios",
                column: "IdEstado");

            migrationBuilder.CreateIndex(
                name: "IX_direcciones_usuarios_IdMunicipio",
                table: "direcciones_usuarios",
                column: "IdMunicipio");

            migrationBuilder.CreateIndex(
                name: "IX_direcciones_usuarios_IdUsuario",
                table: "direcciones_usuarios",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_direcciones_usuarios_IdUsuario_EsPredeterminada",
                table: "direcciones_usuarios",
                columns: new[] { "IdUsuario", "EsPredeterminada" });

            migrationBuilder.CreateIndex(
                name: "IX_direcciones_usuarios_Uuid",
                table: "direcciones_usuarios",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_FechaCreacion",
                table: "notificaciones",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_IdUsuario",
                table: "notificaciones",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_Leida",
                table: "notificaciones",
                column: "Leida");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_TipoReferencia_IdReferencia",
                table: "notificaciones",
                columns: new[] { "TipoReferencia", "IdReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_Uuid",
                table: "notificaciones",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedido_detalles_IdPedido",
                table: "pedido_detalles",
                column: "IdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_detalles_IdProductoServicio",
                table: "pedido_detalles",
                column: "IdProductoServicio");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_detalles_Uuid",
                table: "pedido_detalles",
                column: "Uuid",
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

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_Estatus",
                table: "pedidos",
                column: "Estatus");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_FechaCreacion",
                table: "pedidos",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_Folio",
                table: "pedidos",
                column: "Folio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_IdComercio",
                table: "pedidos",
                column: "IdComercio");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_IdDireccionUsuario",
                table: "pedidos",
                column: "IdDireccionUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_IdUsuario",
                table: "pedidos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_Uuid",
                table: "pedidos",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carrito_detalles");

            migrationBuilder.DropTable(
                name: "comisiones");

            migrationBuilder.DropTable(
                name: "comprobantes_pago");

            migrationBuilder.DropTable(
                name: "configuracion_comercio_pedidos");

            migrationBuilder.DropTable(
                name: "configuracion_comisiones");

            migrationBuilder.DropTable(
                name: "cuentas_bancarias_comercio");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "pedido_detalles");

            migrationBuilder.DropTable(
                name: "pedido_historial");

            migrationBuilder.DropTable(
                name: "carritos");

            migrationBuilder.DropTable(
                name: "pedidos");

            migrationBuilder.DropTable(
                name: "direcciones_usuarios");
        }
    }
}
