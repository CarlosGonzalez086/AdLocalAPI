namespace AdLocalAPI.DTOs
{
    public class ReconciliacionResumenDto
    {
        public int TotalRevisadas { get; set; }
        public int TotalActualizadas { get; set; }
        public int Errores { get; set; }
        public List<string> Detalles { get; set; } = new();
    }
}
