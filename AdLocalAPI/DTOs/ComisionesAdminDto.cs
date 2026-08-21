namespace AdLocalAPI.DTOs
{
    public class ComisionDiaDto { public DateTime Fecha { get; set; } public string Dia { get; set; } = string.Empty; public decimal Monto { get; set; } }
    public class ComisionesDashboardDto
    {
        public decimal ComisionesSemana { get; set; }
        public decimal ComisionesMes { get; set; }
        public decimal PendienteCobro { get; set; }
        public decimal CobradoMes { get; set; }
        public List<ComisionDiaDto> Semana { get; set; } = new();
    }
    public class ComisionComercioResumenDto
    {
        public long ComercioId { get; set; }
        public Guid ComercioUuid { get; set; }
        public string Comercio { get; set; } = string.Empty;
        public int Ventas { get; set; }
        public decimal VentasMonto { get; set; }
        public decimal ComisionGenerada { get; set; }
        public decimal PendientePago { get; set; }
        public decimal PendienteEfectivo { get; set; }
        public decimal PendienteTransferencia { get; set; }
        public DateTime? UltimaVenta { get; set; }
    }
    public class ComisionMovimientoDto
    {
        public Guid Uuid { get; set; }
        public string Comercio { get; set; } = string.Empty;
        public Guid PedidoUuid { get; set; }
        public string NumeroPedido { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = string.Empty;
        public decimal MontoVenta { get; set; }
        public decimal Porcentaje { get; set; }
        public decimal ComisionFija { get; set; }
        public decimal MontoComision { get; set; }
        public int Estatus { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? FechaPago { get; set; }
    }
    public class LiquidarComisionesDto { public string Periodo { get; set; } = "semana"; }
}
