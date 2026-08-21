using AdLocalAPI.Models;

namespace AdLocalAPI.DTOs.UsuarioCliente.Checkout
{
    public class CheckoutCarritoItemDto
    {
        public long IdDetalleCarrito { get; set; }

        public Guid DetalleUuid { get; set; }

        public long IdProductoServicio { get; set; }

        public int Cantidad { get; set; }

        public string? Observaciones { get; set; }

        public ProductosServicios Producto { get; set; } = null!;

        public Comercio Comercio { get; set; } = null!;
    }
}
