namespace AdLocalAPI.DTOs
{
    public class PlanCreateDto
    {
        public string Nombre { get; set; }
        public string StripePriceId { get; set; }
        public decimal Precio { get; set; }
        public int DuracionDias { get; set; }
        public string Tipo { get; set; }

        public int MaxNegocios { get; set; }
        public int MaxProductos { get; set; }
        public int MaxFotos { get; set; }

        public int NivelVisibilidad { get; set; }
        public bool PermiteCatalogo { get; set; }
        public bool ColoresPersonalizados { get; set; }
        public bool TieneBadge { get; set; }
        public string? BadgeTexto { get; set; }
        public bool TieneAnalytics { get; set; }
        public bool IsMultiUsuario { get; set; }
    }

}
