namespace AdLocalAPI.DTOs.Carrito
{
    public class CarritoDetalleResponseDto
    {
        public Guid Uuid { get; set; }

        public Guid ProductoUuid { get; set; }

        // Comercio al que pertenece el producto
        public long IdComercio { get; set; }

        public Guid ComercioUuid { get; set; }

        public string ComercioNombre { get; set; } = string.Empty;

        public string? ComercioLogoUrl { get; set; }

        // Producto
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public string? LogoUrl { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }

        public string? Observaciones { get; set; }

        public bool Disponible { get; set; }

        public bool ManejaStock { get; set; }

        public int? Stock { get; set; }

        public bool PermiteDomicilio { get; set; }

        public bool PermiteRecoger { get; set; }
    }
}