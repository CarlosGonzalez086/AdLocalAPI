using AdLocalAPI.Utils;

namespace AdLocalAPI.DTOs;

public class PedidoReembolsoDto
{
    public Guid Uuid { get; set; }
    public decimal Monto { get; set; }
    public MetodoPagoPedido Metodo { get; set; }
    public string Referencia { get; set; } = "";
    public string Motivo { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
}

public class RegistrarReembolsoPedidoDto
{
    [System.Text.Json.Serialization.JsonRequired]
    public long VersionPedido { get; set; }
    public Guid IdempotencyKey { get; set; }
    public decimal Monto { get; set; }
    public MetodoPagoPedido Metodo { get; set; }
    public string Referencia { get; set; } = "";
    public string Motivo { get; set; } = "";
    public List<DevolucionPedidoProductoDto> ProductosDevueltos { get; set; } = new();
}

public class DevolucionPedidoProductoDto
{
    public Guid DetalleUuid { get; set; }
    public int Cantidad { get; set; }
}

public class ConciliarInventarioPedidoDto
{
    [System.Text.Json.Serialization.JsonRequired]
    public long VersionPedido { get; set; }
    public string Referencia { get; set; } = "";
    public bool? RestituirReserva { get; set; }
    public List<DevolucionPedidoProductoDto> StockDescontado { get; set; } = new();
}
