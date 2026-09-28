namespace AdLocalAPI.DTOs
{
    public class ComercioVisitaDTOs
    {
        public class ComercioVisitasStatsDto
        {
            public List<VisitasPorDiaDto> UltimaSemana { get; set; } = new();
            public List<VisitasPorMesDto> UltimosTresMeses { get; set; } = new();
        }

        public class VisitasPorDiaDto
        {
            public string Dia { get; set; } = string.Empty;   
            public int Total { get; set; }
            public DateTime Fecha { get; set; }
        }

        public class VisitasPorMesDto
        {
            public string Mes { get; set; } = string.Empty; 
            public int Total { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }
        }
    }
}
