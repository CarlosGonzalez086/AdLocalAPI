using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Models;

[Table("pedido_reembolsos")]
public class PedidoReembolso
{
    [Key] public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long IdPedido { get; set; }
    public Guid IdempotencyKey { get; set; }
    [Required, MaxLength(64)] public string RequestHash { get; set; } = "";
    [Column(TypeName = "numeric(18,2)")] public decimal Monto { get; set; }
    public MetodoPagoPedido Metodo { get; set; }
    [Required, MaxLength(150)] public string Referencia { get; set; } = "";
    [Required, MaxLength(500)] public string Motivo { get; set; } = "";
    public long IdUsuarioRegistro { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Pedido Pedido { get; set; } = null!;
}

[Table("pedido_movimientos_inventario")]
public class PedidoMovimientoInventario
{
    [Key] public long Id { get; set; }
    public long IdPedidoDetalle { get; set; }
    public int Cantidad { get; set; }
    public long IdUsuario { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public PedidoDetalle Detalle { get; set; } = null!;
}
