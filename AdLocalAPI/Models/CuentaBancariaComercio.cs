using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("cuentas_bancarias_comercio")]
    public class CuentaBancariaComercio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdComercio { get; set; }

        [Required]
        [MaxLength(100)]
        public string Banco { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Beneficiario { get; set; } = string.Empty;

        [MaxLength(25)]
        public string? NumeroCuenta { get; set; }

        [MaxLength(18)]
        public string? Clabe { get; set; }

        [MaxLength(19)]
        public string? NumeroTarjeta { get; set; }

        [Required]
        public bool Principal { get; set; } = false;

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        [ForeignKey(nameof(IdComercio))]
        public Comercio Comercio { get; set; } = null!;
    }
}