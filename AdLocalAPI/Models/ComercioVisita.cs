using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("comercio_visitas")]
    public class ComercioVisita
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public long ComercioId { get; set; }
        public DateTime FechaVisita { get; set; } = DateTime.UtcNow;
        public string? Ip { get; set; }
    }
}
