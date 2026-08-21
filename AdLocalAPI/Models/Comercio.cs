using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    public class Comercio
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public long IdUsuario { get; set; }

        [MaxLength(1000)]
        public string? Descripcion { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Direccion { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        public Point? Ubicacion { get; set; }

        [MaxLength(7)]
        public string? ColorPrimario { get; set; }

        [MaxLength(7)]
        public string? ColorSecundario { get; set; }

        [Required]
        public int EstadoId { get; set; }

        [Required]
        public int MunicipioId { get; set; }

        [ForeignKey(nameof(EstadoId))]
        public Estado Estado { get; set; } = null!;

        [ForeignKey(nameof(MunicipioId))]
        public Municipio Municipio { get; set; } = null!;

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        public bool Visible { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        public ICollection<CalificacionComentario> CalificacionesComentarios { get; set; } = new List<CalificacionComentario>();

        public Usuario Usuario { get; set; } = null!;

        public long? TipoComercioId { get; set; }

        public TipoComercio? TipoComercio { get; set; }
    }
}