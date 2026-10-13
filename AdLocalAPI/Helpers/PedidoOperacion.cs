using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace AdLocalAPI.Helpers;

public sealed class PedidoOperacionException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public static class PedidoOperacion
{
    public static bool Cerrado(EstadoPedido estado) => estado is EstadoPedido.Cancelado or EstadoPedido.Rechazado;
    public static bool Entregado(EstadoPedido estado) => estado is EstadoPedido.Entregado or EstadoPedido.Completado;
    public static bool RequiereDevolucionFisica(Pedido p) => p.FechaEntrega.HasValue || p.Estado is
        EstadoPedido.Preparando or EstadoPedido.ListoParaRecoger or EstadoPedido.ListoParaEnviar or
        EstadoPedido.Enviado or EstadoPedido.Entregado or EstadoPedido.Completado;
    public static void DeclararDevolucion(Pedido p, List<DevolucionPedidoProductoDto> productos)
    {
        Exigir(productos.All(x => x != null), "Los productos devueltos no pueden ser nulos.", "400");
        Exigir(productos.Select(x => x.DetalleUuid).Distinct().Count() == productos.Count, "No repitas productos devueltos.", "400");
        foreach (var dato in productos)
        {
            var d = p.Detalles.SingleOrDefault(x => x.Uuid == dato.DetalleUuid);
            Exigir(d != null && dato.Cantidad > 0 && dato.Cantidad <= d.StockDescontado,
                "La devolución supera el stock descontado o requiere conciliar el pedido antiguo.", "400");
            d!.StockPorRestituir = dato.Cantidad;
        }
    }
    public static void Exigir([DoesNotReturnIf(false)] bool condicion, string mensaje, string codigo = "409")
    {
        if (!condicion) throw new PedidoOperacionException(codigo, mensaje);
    }
    public static void Version(Pedido pedido, long version) => Exigir(pedido.Version == version,
        "El pedido cambió. Actualiza el detalle antes de realizar esta operación.");
    public static bool PuedeRegistrarPago(Pedido pedido) => !Cerrado(pedido.Estado) &&
        pedido.EstadoPago is EstadoPagoPedido.Pendiente or EstadoPagoPedido.Rechazado or EstadoPagoPedido.PendienteVerificacion;

    public static List<EstadoPedido> Acciones(EstadoPedido estado, TipoEntregaPedido entrega, EstadoPagoPedido? pago = null)
    {
        if (pago == EstadoPagoPedido.Reembolsado) return new();
        List<EstadoPedido> acciones = estado switch
        {
            EstadoPedido.PendienteAprobacion => [EstadoPedido.Aprobado, EstadoPedido.Rechazado],
            EstadoPedido.Aprobado => [EstadoPedido.Preparando, EstadoPedido.Cancelado],
            EstadoPedido.Preparando => [entrega == TipoEntregaPedido.Domicilio ? EstadoPedido.ListoParaEnviar : EstadoPedido.ListoParaRecoger, EstadoPedido.Cancelado],
            EstadoPedido.ListoParaEnviar when entrega == TipoEntregaPedido.Domicilio => [EstadoPedido.Enviado, EstadoPedido.Cancelado],
            EstadoPedido.Enviado when entrega == TipoEntregaPedido.Domicilio => [EstadoPedido.Entregado],
            EstadoPedido.ListoParaRecoger when entrega == TipoEntregaPedido.Recoger => [EstadoPedido.Entregado, EstadoPedido.Cancelado],
            EstadoPedido.Entregado => [EstadoPedido.Completado],
            _ => new()
        };
        if (pago == EstadoPagoPedido.Pagado || pago == EstadoPagoPedido.PendienteVerificacion)
            acciones.RemoveAll(Cerrado);
        if (pago.HasValue && pago != EstadoPagoPedido.Pagado)
            acciones.RemoveAll(x => x is EstadoPedido.Enviado or EstadoPedido.Entregado or EstadoPedido.Completado);
        return acciones;
    }
    public static void ValidarEstado(Pedido pedido, CambiarEstadoPedidoDto dto)
    {
        Version(pedido, dto.VersionPedido);
        if (Cerrado(dto.Estado) && pedido.EstadoPago == EstadoPagoPedido.Pagado)
            throw new PedidoOperacionException("409", "Un pedido pagado debe reembolsarse antes de rechazarse o cancelarse.");
        Exigir(Acciones(pedido.Estado, pedido.TipoEntrega, pedido.EstadoPago).Contains(dto.Estado),
            "La transición de estado no está permitida.");
        if (Cerrado(dto.Estado)) Exigir(!string.IsNullOrWhiteSpace(dto.Comentario), "Escribe el motivo de cancelación o rechazo.", "400");
        Exigir(dto.Comentario?.Length is not > 500, "El comentario no puede exceder 500 caracteres.", "400");
    }
    public static string HashReembolso(RegistrarReembolsoPedidoDto dto) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Monto = dto.Monto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), dto.Metodo,
            Referencia = dto.Referencia.Trim(), Motivo = dto.Motivo.Trim(),
            Productos = dto.ProductosDevueltos.OrderBy(x => x.DetalleUuid).Select(x => new { x.DetalleUuid, x.Cantidad }) }))));
}
