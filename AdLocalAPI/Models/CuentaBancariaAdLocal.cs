using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("cuentas_bancarias_adlocal")]
    public class CuentaBancariaAdLocal
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public long Id { get; set; }
        [Required] public Guid Uuid { get; set; } = Guid.NewGuid();
        [Required, MaxLength(100)] public string Banco { get; set; } = string.Empty;
        [Required, MaxLength(150)] public string Beneficiario { get; set; } = string.Empty;
        [MaxLength(30)] public string? NumeroCuenta { get; set; }
        [MaxLength(18)] public string? Clabe { get; set; }
        [MaxLength(19)] public string? NumeroTarjeta { get; set; }
        [MaxLength(500)] public string? Instrucciones { get; set; }
        public bool Principal { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
    }
}
