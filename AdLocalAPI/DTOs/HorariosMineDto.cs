namespace AdLocalAPI.DTOs
{
    public class HorariosMineDto
    {
        public int Id { get; set; }

        public int ComercioId { get; set; }

        public DayOfWeek Dia { get; set; }
        public bool Abierto { get; set; }
        public TimeSpan? HoraApertura { get; set; }
        public TimeSpan? HoraCierre { get; set; }

        public string HoraAperturaFormateada => HoraApertura.HasValue
            ? HoraApertura.Value.ToString(@"hh\:mm")
            : "--";

        public string HoraCierreFormateada => HoraCierre.HasValue
            ? HoraCierre.Value.ToString(@"hh\:mm")
            : "--";


        public string RangoHoras => Abierto
            ? $"{HoraAperturaFormateada} – {HoraCierreFormateada}"
            : "Cerrado";
    }


}
