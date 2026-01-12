namespace AdLocalAPI.DTOs
{
    public class HorarioComercioDto
    {
        public DayOfWeek Dia { get; set; }
        public bool Abierto { get; set; }
        public TimeSpan? HoraApertura { get; set; }
        public TimeSpan? HoraCierre { get; set; }
    }
}
