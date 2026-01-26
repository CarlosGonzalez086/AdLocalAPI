using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Evento
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Titulo { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [MaxLength(200)]
        public string Lugar { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        // 🔐 Dueño
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
    }
}
