using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("horarios_cita_servicio")]
    public class HorarioCitaServicio
    {
        [Key] public long Id { get; set; }
        [Required] public Guid Uuid { get; set; } = Guid.NewGuid();
        [Required] public long IdProductoServicio { get; set; }
        [Required] public long IdComercio { get; set; }
        [Required] public DateOnly Fecha { get; set; }
        [Required] public TimeSpan HoraInicio { get; set; }
        [Required] public TimeSpan HoraFin { get; set; }
        public bool Disponible { get; set; } = true;
        public long? IdCita { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
