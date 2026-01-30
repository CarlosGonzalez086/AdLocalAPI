using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("UsosCodigoReferido")]
    public class UsoCodigoReferido
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long UsuarioReferidorId { get; set; }

        [Required]
        public long UsuarioReferidoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodigoReferido { get; set; }

        public DateTime FechaUso { get; set; } = DateTime.UtcNow;

    }
}
