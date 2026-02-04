namespace AdLocalAPI.DTOs
{
    public class SuscripcionListadoDto
    {
        public int Id { get; set; }

        public string Estado { get; set; } = null!;

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public bool AutoRenew { get; set; }

        public string UsuarioNombre { get; set; } = null!;
        public string UsuarioEmail { get; set; } = null!;

        public string PlanNombre { get; set; } = null!;
        public string PlanTipo { get; set; } = null!;
        public decimal Precio { get; set; }
    }
}
