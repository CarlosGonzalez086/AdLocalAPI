using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Plan
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } // Ej: Mensual, Anual

        [Required]
        public decimal Precio { get; set; } // Precio del plan
        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } // Basico | Premium | Empresarial

        [Required]
        public int DuracionDias { get; set; } // 30 para mensual, 365 para anual

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
