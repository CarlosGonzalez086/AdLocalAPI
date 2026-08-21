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
        public Guid Uuid { get; set; } = Guid.NewGuid();

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

        [Required]
        [Column("modalidad")]
        public ModalidadProductoServicio Modalidad { get; set; } = ModalidadProductoServicio.Compra;

        [Column("precio", TypeName = "numeric(18,2)")]
        public decimal? Precio { get; set; }

        [Column("precio_desde", TypeName = "numeric(18,2)")]
        public decimal? PrecioDesde { get; set; }

        [Column("maneja_stock")]
        public bool ManejaStock { get; set; } = false;

        [Column("stock")]
        public int? Stock { get; set; }

        [Column("disponible")]
        public bool Disponible { get; set; } = true;

        [Column("permite_domicilio")]
        public bool PermiteDomicilio { get; set; } = true;

        [Column("permite_recoger")]
        public bool PermiteRecoger { get; set; } = true;

        [Column("duracion_minutos")]
        public int? DuracionMinutos { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("eliminado")]
        public bool Eliminado { get; set; } = false;

        [Column("visible")]
        public bool Visible { get; set; } = true;

        [MaxLength(100)]
        [Column("codigo_interno")]
        public string? CodigoInterno { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_actualizacion")]
        public DateTime? FechaActualizacion { get; set; }

        [Column("fecha_eliminado")]
        public DateTime? FechaEliminado { get; set; }
    }

    public enum TipoProductoServicio
    {
        Producto = 1,
        Servicio = 2
    }

    public enum ModalidadProductoServicio
    {
        Compra = 1,
        Reservacion = 2,
        Cotizacion = 3
    }
}