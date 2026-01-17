using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace AdLocalAPI.Models
{
    public class Comercio
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }
        [Required]
        public long IdUsuario { get; set; }
        public string? Descripcion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? LogoUrl { get; set; }
        public Point? Ubicacion { get; set; }
        [MaxLength(7)]
        public string? ColorPrimario { get; set; }  

        [MaxLength(7)]
        public string? ColorSecundario { get; set; }

        // 🔹 NUEVOS CAMPOS
        [Required]
        public int EstadoId { get; set; }

        [Required]
        public int MunicipioId { get; set; }

        // 🔹 Navegación
        [ForeignKey(nameof(EstadoId))]
        public Estado Estado { get; set; }

        [ForeignKey(nameof(MunicipioId))]
        public Municipio Municipio { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
