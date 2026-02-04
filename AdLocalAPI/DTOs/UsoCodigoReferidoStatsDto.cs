namespace AdLocalAPI.DTOs
{
    public class UsoCodigoReferidoStatsDto
    {
        public long UsuarioReferidorId { get; set; }
        public string CodigoReferido { get; set; } = null!;
        public int TotalUsos { get; set; }
    }
}
