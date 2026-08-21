using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("direcciones_usuarios")]
    public class DireccionUsuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        // =========================
        // USUARIO
        // =========================

        [Required]
        public long IdUsuario { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public Usuario Usuario { get; set; } = null!;

        // =========================
        // IDENTIFICACIÓN
        // =========================

        [Required]
        [MaxLength(50)]
        public string Alias { get; set; } = string.Empty;

        // =========================
        // DIRECCIÓN
        // =========================

        [Required]
        [MaxLength(200)]
        public string Calle { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NumeroExterior { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? NumeroInterior { get; set; }

        [Required]
        [MaxLength(150)]
        public string Colonia { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string CodigoPostal { get; set; } = string.Empty;

        // =========================
        // ESTADO / MUNICIPIO
        // =========================

        [Required]
        public int IdEstado { get; set; }

        [Required]
        public int IdMunicipio { get; set; }

        [ForeignKey(nameof(IdEstado))]
        public Estado Estado { get; set; } = null!;

        [ForeignKey(nameof(IdMunicipio))]
        public Municipio Municipio { get; set; } = null!;

        // =========================
        // UBICACIÓN
        // =========================

        [Column(TypeName = "numeric(10,7)")]
        public decimal? Latitud { get; set; }

        [Column(TypeName = "numeric(10,7)")]
        public decimal? Longitud { get; set; }

        // =========================
        // REFERENCIAS / CONTACTO
        // =========================

        [MaxLength(500)]
        public string? Referencias { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        // =========================
        // CONFIGURACIÓN
        // =========================

        public bool EsPredeterminada { get; set; } = false;

        public bool Activo { get; set; } = true;

        public bool Eliminado { get; set; } = false;

        // =========================
        // FECHAS
        // =========================

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        public DateTime? FechaEliminado { get; set; }
    }
}