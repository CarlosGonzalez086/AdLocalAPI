using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class HorarioComercio
    {
        public int Id { get; set; }
        [Required]
        public long ComercioId { get; set; }
        [Required]
        public DayOfWeek Dia { get; set; } 
        public bool Abierto { get; set; }
        public TimeSpan? HoraApertura { get; set; }
        public TimeSpan? HoraCierre { get; set; }


    }
}
