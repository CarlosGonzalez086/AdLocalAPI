namespace AdLocalAPI.DTOs.Carrito
{
    public class ActualizarCantidadCarritoDto
    {
        public Guid DetalleUuid { get; set; }
        public int Cantidad { get; set; }
    }
}