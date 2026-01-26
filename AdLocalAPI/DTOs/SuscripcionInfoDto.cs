namespace AdLocalAPI.DTOs
{
    public class SuscripcionInfoDto
    {
        public int Id { get; set; }

        public PlanInfoDto Plan { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public bool Activa { get; set; }
        public string Estado { get; set; }

        public decimal Monto { get; set; }
        public string Moneda { get; set; }
    }


}
