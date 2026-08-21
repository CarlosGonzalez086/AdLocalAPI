using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("citas")]
    public class Cita
    {
        [Key] public long Id { get; set; }
        [Required] public Guid Uuid { get; set; } = Guid.NewGuid();
        [Required] public long IdUsuario { get; set; }
        [Required] public long IdComercio { get; set; }
        [Required] public long IdProductoServicio { get; set; }
        [Required, MaxLength(150)] public string NombrePersona { get; set; } = string.Empty;
        [MaxLength(500)] public string? NotasCliente { get; set; }
        [MaxLength(150)] public string? NombreAtiende { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }
        public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;
        [MaxLength(500)] public string? MotivoCancelacion { get; set; }
        [Column("FechaCreacion", TypeName = "timestamp with time zone")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        [Column("FechaActualizacion", TypeName = "timestamp with time zone")]
        public DateTime? FechaActualizacion { get; set; }
    }

    public enum EstadoCita { Pendiente = 1, Confirmada = 2, EnAtencion = 3, Completada = 4, Cancelada = 5, NoAsistio = 6 }
}
