namespace AdLocalAPI.DTOs
{
    public class ComercioMineDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
        public string? LogoBase64 { get; set; }

        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }
}
