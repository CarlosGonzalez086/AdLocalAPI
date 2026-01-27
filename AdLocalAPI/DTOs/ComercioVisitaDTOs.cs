namespace AdLocalAPI.DTOs
{
    public class ComercioVisitaDTOs
    {
        public class ComercioVisitasStatsDto
        {
            public List<VisitasPorDiaDto> UltimaSemana { get; set; }
            public List<VisitasPorMesDto> UltimosTresMeses { get; set; }
        }

        public class VisitasPorDiaDto
        {
            public string Dia { get; set; }   
            public int Total { get; set; }
            public DateTime Fecha { get; set; }
        }

        public class VisitasPorMesDto
        {
            public string Mes { get; set; } 
            public int Total { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }
        }
    }
}
