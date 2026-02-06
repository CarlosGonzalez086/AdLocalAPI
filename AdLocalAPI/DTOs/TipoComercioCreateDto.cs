namespace AdLocalAPI.DTOs
{
    public class TipoComercioCreateDto
    {
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class TipoComercioDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }
}
