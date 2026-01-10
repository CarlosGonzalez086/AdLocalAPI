namespace AdLocalAPI.DTOs
{
    public class ProductosServiciosDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        public int Tipo { get; set; } = 1;
        public decimal? Precio { get; set; }
        public int? Stock { get; set; }
        public string? ImagenBase64 { get; set; }

        public bool Activo { get; set; } = true;
    }
}
