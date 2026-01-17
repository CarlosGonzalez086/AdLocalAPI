using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("estados_municipios")]
    public class EstadoMunicipio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("estados_id")]
        public int EstadoId { get; set; }

        [Column("municipios_id")]
        public int MunicipioId { get; set; }
        // Navegación
        public Estado Estado { get; set; }
        public Municipio Municipio { get; set; }

    }
}
