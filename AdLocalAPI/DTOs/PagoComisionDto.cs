namespace AdLocalAPI.DTOs
{
    public class EstadoComisionesComercioDto
    {
        public long ComercioId { get; set; }
        public string Comercio { get; set; } = string.Empty;
        public decimal PendienteSemana { get; set; }
        public decimal PendienteMes { get; set; }
        public PagoComisionListadoDto? PagoEnRevision { get; set; }
    }
    public class CrearPagoComisionDto
    {
        public long ComercioId { get; set; }
        public Guid CuentaBancariaUuid { get; set; }
        public string Periodo { get; set; } = "semana";
        public string MetodoPago { get; set; } = "transferencia";
        public string ComprobanteBase64 { get; set; } = string.Empty;
    }
    public class RevisarPagoComisionDto { public bool Aprobar { get; set; } public string? Comentario { get; set; } }
    public class PagoComisionListadoDto
    {
        public Guid Uuid { get; set; }
        public long ComercioId { get; set; }
        public string Comercio { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public int Estatus { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int ComisionesIncluidas { get; set; }
    }
}
