using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Plan
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } // Free, Básico, Pro, Business
        [Required]
        [MaxLength(100)]
        public string StripePriceId { get; set; } = null!;

        [Required]
        public decimal Precio { get; set; } // 0 para Free

        [Required]
        public int DuracionDias { get; set; } // 30, 365

        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } // FREE | BASIC | PRO | BUSINESS

        // Capacidades
        public int MaxNegocios { get; set; }
        public int MaxProductos { get; set; }
        public int MaxFotos { get; set; }

        // Visibilidad / features
        public int NivelVisibilidad { get; set; } // 0 a 100
        public bool PermiteCatalogo { get; set; }
        public bool ColoresPersonalizados { get; set; }
        public bool TieneBadge { get; set; }
        public string? BadgeTexto { get; set; }
        public bool TieneAnalytics { get; set; }
        public bool IsMultiUsuario { get; set; } = false;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

}
