using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Models;

namespace AdLocalAPI.Helpers;

public static class CheckoutOperacion
{
    public static readonly TimeSpan EsperaLegado = TimeSpan.FromMinutes(10);

    private static string Hash(object value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));

    public static string Solicitud(ConfirmarCheckoutDto dto) => Hash(new
    {
        dto.CarritoUuid, dto.CarritoVersion,
        Comercios = dto.Comercios.OrderBy(x => x.ComercioUuid).Select(x => new
        {
            x.ComercioUuid, x.TipoEntrega, x.MetodoPago, x.DireccionUuid,
            Observaciones = string.IsNullOrWhiteSpace(x.Observaciones) ? null : x.Observaciones.Trim()
        })
    });

    public static string Carrito(IEnumerable<CheckoutCarritoItemDto> items) => Hash(
        items.OrderBy(x => x.IdDetalleCarrito).Select(x => new
        {
            x.IdDetalleCarrito, x.IdProductoServicio, x.Cantidad, x.Observaciones,
            x.Producto.Precio, x.Producto.IdComercio, x.Producto.ManejaStock,
            x.Producto.PermiteDomicilio, x.Producto.PermiteRecoger,
            x.Producto.Activo, x.Producto.Visible, x.Producto.Disponible,
            x.Producto.Eliminado, x.Producto.Modalidad,
            ComercioActivo = x.Comercio.Activo, ComercioVisible = x.Comercio.Visible
        }));

    public static void ValidarSolicitud(CheckoutIdempotencia existente, string? hash)
    {
        if (existente.RequestHash != hash || hash == null)
            throw new CheckoutConflictException("La clave pertenece a una solicitud diferente o antigua. Revisa tus pedidos antes de iniciar otra compra.");
    }
}

public sealed class CheckoutValidationException(string message) : Exception(message);
public sealed class CheckoutConflictException(string message) : Exception(message);
public sealed record CheckoutGuardado(string ResponseJson, bool Creado);
