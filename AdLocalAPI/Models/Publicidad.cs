using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Publicidad
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Titulo { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [MaxLength(300)]
        public string ImagenUrl { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        // 🔐 Dueño
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
    }
}
