using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    public class RelComercioImagen
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("id_comercio")]
        public long IdComercio { get; set; }

        [Required]
        [Column("foto_url")]
        public string FotoUrl { get; set; } = string.Empty;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_actualizacion")]
        public DateTime? FechaActualizacion { get; set; }
    }
}
