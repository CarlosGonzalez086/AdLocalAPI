using AdLocalAPI.Utils;

namespace AdLocalAPI.DTOs.UsuarioCliente.Checkout
{
    public class ConfirmarCheckoutDto
    {
        public List<CheckoutComercioDto> Comercios { get; set; }
            = new();
    }

    public class CheckoutComercioDto
    {
        public Guid ComercioUuid { get; set; }

        public TipoEntregaPedido TipoEntrega { get; set; }

        public MetodoPagoPedido MetodoPago { get; set; }

        /*
         * Obligatorio solamente para domicilio.
         */
        public Guid? DireccionUuid { get; set; }

        public string? Observaciones { get; set; }
    }
    public class ConfirmarCheckoutResponseDto
    {
        public int TotalPedidos { get; set; }

        public decimal TotalGeneral { get; set; }

        public List<PedidoCheckoutResponseDto> Pedidos { get; set; }
            = new();
    }

    public class PedidoCheckoutResponseDto
    {
        public Guid Uuid { get; set; }

        public string NumeroPedido { get; set; } = string.Empty;

        public Guid ComercioUuid { get; set; }

        public string Comercio { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public int Estado { get; set; }

        public int EstadoPago { get; set; }

        public int MetodoPago { get; set; }

        public int TipoEntrega { get; set; }

        public bool RequiereComprobante { get; set; }
    }

    public class ComprobanteTransferenciaResponseDto
    {
        public Guid PedidoUuid { get; set; }

        public Guid ComprobanteUuid { get; set; }

        public int EstadoPago { get; set; }

        public DateTime FechaCarga { get; set; }
    }

    public class SubirComprobanteTransferenciaDto
    {
        public string ArchivoBase64 { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
    }
    public class CheckoutResponseDto
    {
        public decimal TotalGeneral { get; set; }

        public List<CheckoutComercioResponseDto> Comercios { get; set; }
            = new();
    }

    public class CheckoutComercioResponseDto
    {
        public Guid ComercioUuid { get; set; }

        public string Comercio { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        public decimal Subtotal { get; set; }

        public bool AceptaEfectivo { get; set; }

        public bool AceptaTransferencia { get; set; }

        public bool PermiteDomicilio { get; set; }

        public bool PermiteRecoger { get; set; }

        public string? InstruccionesTransferencia { get; set; }

        public CuentaTransferenciaCheckoutDto? CuentaTransferencia { get; set; }

        public List<CheckoutProductoDto> Productos { get; set; }
            = new();
    }

    public class CuentaTransferenciaCheckoutDto
    {
        public string Banco { get; set; } = string.Empty;

        public string Beneficiario { get; set; } = string.Empty;

        public string? NumeroCuenta { get; set; }

        public string? Clabe { get; set; }

        public string? NumeroTarjeta { get; set; }
    }

    public class CheckoutProductoDto
    {
        public Guid ProductoUuid { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }

        public bool PermiteDomicilio { get; set; }

        public bool PermiteRecoger { get; set; }
    }
    public class DireccionCheckoutDto
    {
        public long Id { get; set; }

        public Guid Uuid { get; set; }

        public string Alias { get; set; } = string.Empty;

        public string Calle { get; set; } = string.Empty;

        public string NumeroExterior { get; set; } = string.Empty;

        public string? NumeroInterior { get; set; }

        public string Colonia { get; set; } = string.Empty;

        public string CodigoPostal { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string Municipio { get; set; } = string.Empty;

        public decimal? Latitud { get; set; }

        public decimal? Longitud { get; set; }

        public string? Referencias { get; set; }

        public string? Telefono { get; set; }
    }
}
