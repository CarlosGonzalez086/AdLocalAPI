using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    public class ConfiguracionSistema
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Val { get; set; } = string.Empty;

        [Required]
        public DateTime Actualizado { get; set; }

        [Required]
        [StringLength(100)]
        public string Tipo { get; set; } = string.Empty;
    }
}
