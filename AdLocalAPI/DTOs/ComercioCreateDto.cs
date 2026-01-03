namespace AdLocalAPI.DTOs
{
    public class ComercioCreateDto
    {
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }

        // 🖼️ URL del logo
        public string? LogoBase64 { get; set; }

        // 📍 Coordenadas desde react-leaflet
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
