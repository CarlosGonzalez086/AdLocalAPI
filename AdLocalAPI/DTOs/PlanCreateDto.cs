namespace AdLocalAPI.DTOs
{
    public class PlanCreateDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int DuracionDias { get; set; }
        public string Tipo { get; set; }
    }
}
