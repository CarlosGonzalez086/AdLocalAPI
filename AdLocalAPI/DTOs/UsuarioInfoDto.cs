namespace AdLocalAPI.DTOs
{
    public class UsuarioInfoDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public string Rol { get; set; } = string.Empty;
        public long? ComercioId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
    }
}
