namespace AdLocalAPI.DTOs
{
    public class SuscripcionPorPlanDto
    {
        public string Plan { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public int Total { get; set; }
    }

    public class SuscripcionDashboardDto
    {
        public List<SuscripcionPorPlanDto> PorPlan { get; set; } = new();
        public int UltimaSemana { get; set; }
        public int UltimosTresMeses { get; set; }
    }
}
