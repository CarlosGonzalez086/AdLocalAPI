namespace AdLocalAPI.DTOs
{
    public class UsuarioRegistroDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? CodigoReferenciado { get; set; }
        public int? ComercioId { get; set; } // Opcional
    }
}
