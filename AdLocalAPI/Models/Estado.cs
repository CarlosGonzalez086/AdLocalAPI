using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("estados")]
    public class Estado
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("estado")]
        [StringLength(100)]
        public string EstadoNombre { get; set; }
        public ICollection<EstadoMunicipio> EstadosMunicipios { get; set; }

    }
}
