using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    public class ProductosServicios
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }


        [Required]
        [Column("id_comercio")]
        public long IdComercio { get; set; }

        [Required]
        [Column("id_usuario")]
        public long IdUsuario { get; set; }


        [Required]
        [MaxLength(150)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("LogoUrl")]
        public string? LogoUrl { get; set; }

        [Required]
        [Column("tipo")]
        public TipoProductoServicio Tipo { get; set; }


        [Column("precio", TypeName = "numeric(18,2)")]
        public decimal? Precio { get; set; }

        [Column("stock")]
        public int? Stock { get; set; }


        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("eliminado")]
        public bool Eliminado { get; set; } = false;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_actualizacion")]
        public DateTime? FechaActualizacion { get; set; }

        [Column("fecha_eliminado")]
        public DateTime? FechaEliminado { get; set; }


        [MaxLength(100)]
        [Column("codigo_interno")]
        public string? CodigoInterno { get; set; }

        [Column("visible")]
        public bool Visible { get; set; } = true;
    }
    public enum TipoProductoServicio
    {
        Producto = 1,
        Servicio = 2
    }
}
