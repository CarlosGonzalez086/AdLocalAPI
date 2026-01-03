namespace AdLocalAPI.DTOs
{
    public class UsuarioInfoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string? FotoUrl { get; set; }
        public string Rol { get; set; }
        public int? ComercioId { get; set; } // Solo para usuario/comercio
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
    }
}
