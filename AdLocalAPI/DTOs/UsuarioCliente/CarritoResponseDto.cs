namespace AdLocalAPI.DTOs.Carrito
{
    public class CarritoResponseDto
    {
        public Guid Uuid { get; set; }

        public decimal Subtotal { get; set; }

        public int TotalProductos { get; set; }

        public int TotalComercios { get; set; }

        public DateTime FechaCreacion { get; set; }

        public List<CarritoComercioResponseDto> Comercios { get; set; }
            = new List<CarritoComercioResponseDto>();
    }
}