namespace AdLocalAPI.DTOs.Carrito
{
    public class AgregarProductoCarritoDto
    {
        public Guid ProductoUuid { get; set; }
        public int Cantidad { get; set; } = 1;
        public string? Observaciones { get; set; }
    }
}