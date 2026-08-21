namespace AdLocalAPI.DTOs
{
    public class ProductosServiciosDto
    {
        public long Id { get; set; }

        public Guid Uuid { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public int Tipo { get; set; } = 1;

        public int Modalidad { get; set; } = 1;

        public decimal? Precio { get; set; }

        public decimal? PrecioDesde { get; set; }

        public bool ManejaStock { get; set; } = false;

        public int? Stock { get; set; }

        public bool Disponible { get; set; } = true;

        public bool PermiteDomicilio { get; set; } = true;

        public bool PermiteRecoger { get; set; } = true;

        public int? DuracionMinutos { get; set; }

        public string? ImagenBase64 { get; set; }

        public bool Activo { get; set; } = true;

        public bool Visible { get; set; } = true;

        public string? CodigoInterno { get; set; }

        public long IdComercio { get; set; } = 0;
    }
}