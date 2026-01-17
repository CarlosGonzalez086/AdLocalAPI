using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("municipios")]
    public class Municipio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("municipio")]
        [StringLength(100)]
        public string MunicipioNombre { get; set; }
        public ICollection<EstadoMunicipio> EstadosMunicipios { get; set; }

    }
}
