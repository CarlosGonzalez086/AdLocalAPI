using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models;

[Table("cita_historial")]
public class CitaHistorial
{
    public long Id { get; set; }
    public long IdCita { get; set; }
    public long IdUsuario { get; set; }
    [MaxLength(30)] public string Evento { get; set; } = "Estado";
    public EstadoCita EstadoAnterior { get; set; }
    public EstadoCita EstadoNuevo { get; set; }
    [Column(TypeName = "timestamp without time zone")] public DateTime InicioAnterior { get; set; }
    [Column(TypeName = "timestamp without time zone")] public DateTime InicioNuevo { get; set; }
    [MaxLength(500)] public string? Comentario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
