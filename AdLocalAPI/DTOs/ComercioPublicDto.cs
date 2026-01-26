namespace AdLocalAPI.DTOs
{
    public class ComercioPublicDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }

        public double? Lat { get; set; }
        public double? Lng { get; set; }

        public string? ColorPrimario { get; set; }
        public string? ColorSecundario { get; set; }
        public bool Activo { get; set; }
        public string EstadoNombre { get; set; } = "";
        public string MunicipioNombre { get; set; } = "";
        public string Badge { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
        // 🔹 Nuevo campo para promedio de calificaciones
        public double PromedioCalificacion { get; set; } = 0;
    }
}
