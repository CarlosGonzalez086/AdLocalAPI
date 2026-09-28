using AdLocalAPI.Constants;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using System.Globalization;

namespace AdLocalAPI.Services
{
    public partial class CheckoutService : ICheckoutService
    {
        private readonly IPedidoRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly INotificacionService _notificaciones;
        private readonly ILogger<CheckoutService> _logger;

        public CheckoutService(
            IPedidoRepository repository,
            JwtContext jwtContext,
            INotificacionService notificaciones,
            ILogger<CheckoutService> logger)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _notificaciones = notificaciones;
            _logger = logger;
        }

        // ==========================================
        // OBTENER CHECKOUT
        // ==========================================

        public async Task<ApiResponse<CheckoutResponseDto>> ObtenerCheckout(CancellationToken cancellationToken = default)
        {
            try
            {
                var idUsuario = _jwtContext.GetUserId();
                var carrito = await _repository.ObtenerCarritoActivoAsync(idUsuario);

                if (carrito == null)
                {
                    return ApiResponse<CheckoutResponseDto>.Error("404", "No tienes un carrito activo.");
                }

                var items = await _repository.ObtenerProductosCarritoAsync(carrito.Id);
                if (items.Count == 0)
                {
                    return ApiResponse<CheckoutResponseDto>.Error("400", "Tu carrito está vacío.");
                }

                var response = new CheckoutResponseDto();
                var grupos = items.GroupBy(x => x.Producto.IdComercio);

                foreach (var grupo in grupos)
                {
                    var primero = grupo.First();
                    var comercio = primero.Comercio;
                    var configuracion = await _repository.ObtenerConfiguracionPagoAsync(comercio.Id);

                    if (configuracion == null)
                    {
                        return ApiResponse<CheckoutResponseDto>.Error(
                            "400",
                            $"El comercio {comercio.Nombre} todavía no tiene configurados sus métodos de pago."
                        );
                    }

                    CuentaBancariaComercio? cuenta = null;
                    if (configuracion.AceptaTransferencia)
                    {
                        cuenta = await _repository.ObtenerCuentaPrincipalAsync(comercio.Id);
                    }

                    var productos = grupo.Select(x =>
                    {
                        var precio = x.Producto.Precio ?? 0;
                        return new CheckoutProductoDto
                        {
                            ProductoUuid = x.Producto.Uuid,
                            Nombre = x.Producto.Nombre,
                            LogoUrl = x.Producto.LogoUrl,
                            Cantidad = x.Cantidad,
                            PrecioUnitario = precio,
                            Subtotal = precio * x.Cantidad,
                            PermiteDomicilio = x.Producto.PermiteDomicilio,
                            PermiteRecoger = x.Producto.PermiteRecoger
                        };
                    }).ToList();

                    var subtotal = productos.Sum(x => x.Subtotal);

                    response.Comercios.Add(new CheckoutComercioResponseDto
                    {
                        ComercioUuid = comercio.Uuid,
                        Comercio = comercio.Nombre,
                        LogoUrl = comercio.LogoUrl,
                        Subtotal = subtotal,
                        CostoEnvio = configuracion.CompraMinimaEnvioGratis.HasValue && subtotal >= configuracion.CompraMinimaEnvioGratis.Value ? 0 : configuracion.CostoEnvio,
                        TotalDomicilio = subtotal + (configuracion.CompraMinimaEnvioGratis.HasValue && subtotal >= configuracion.CompraMinimaEnvioGratis.Value ? 0 : configuracion.CostoEnvio),
                        CompraMinimaEnvioGratis = configuracion.CompraMinimaEnvioGratis,
                        AceptaEfectivo = configuracion.AceptaEfectivo,
                        AceptaTransferencia = configuracion.AceptaTransferencia && cuenta != null,
                        PermiteDomicilio = productos.All(x => x.PermiteDomicilio),
                        PermiteRecoger = productos.All(x => x.PermiteRecoger),
                        InstruccionesTransferencia = configuracion.InstruccionesTransferencia,
                        CuentaTransferencia = cuenta == null
                            ? null
                            : new CuentaTransferenciaCheckoutDto
                            {
                                Banco = cuenta.Banco,
                                Beneficiario = cuenta.Beneficiario,
                                NumeroCuenta = cuenta.NumeroCuenta,
                                Clabe = cuenta.Clabe,
                                NumeroTarjeta = cuenta.NumeroTarjeta
                            },
                        Productos = productos
                    });

                    response.TotalGeneral += subtotal;
                }

                return ApiResponse<CheckoutResponseDto>.Success(
                    response,
                    "Checkout obtenido correctamente."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<CheckoutResponseDto>.Error("500", ex.Message);
            }
        }

        // ==========================================
        // CONFIGURACIÓN DECIMAL
        // ==========================================

        private async Task<decimal> ObtenerDecimalConfiguracion(string key)
        {
            var config = await _repository.ObtenerConfiguracionAsync(key);
            if (config == null)
            {
                return 0;
            }

            return decimal.TryParse(
                config.Val,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var value
            ) ? value : 0;
        }

        // ==========================================
        // CONFIGURACIÓN BOOLEAN
        // ==========================================

        private async Task<bool> ObtenerBooleanConfiguracion(string key)
        {
            var config = await _repository.ObtenerConfiguracionAsync(key);
            if (config == null)
            {
                return false;
            }

            return bool.TryParse(config.Val, out var value) && value;
        }

        // ==========================================
        // NÚMERO DE PEDIDO
        // ==========================================

        private static string GenerarNumeroPedido()
        {
            return $"ADL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
    }
}
