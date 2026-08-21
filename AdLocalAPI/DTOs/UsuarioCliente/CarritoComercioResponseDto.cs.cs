namespace AdLocalAPI.DTOs.Carrito
{
    public class CarritoComercioResponseDto
    {
        public long IdComercio { get; set; }

        public Guid ComercioUuid { get; set; }

        public string Comercio { get; set; } = string.Empty;

        public string? ComercioLogoUrl { get; set; }

        public int TotalProductos { get; set; }

        public decimal Subtotal { get; set; }

        public List<CarritoDetalleResponseDto> Productos { get; set; }
            = new List<CarritoDetalleResponseDto>();
    }
}