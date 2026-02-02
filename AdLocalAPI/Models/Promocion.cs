using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Promocion
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

        public decimal? PrecioNormal { get; set; }
        public decimal? PrecioPromocion { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
    }
}
