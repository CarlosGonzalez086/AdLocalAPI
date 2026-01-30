namespace AdLocalAPI.DTOs
{
    public class UsuarioRegistroDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? CodigoReferenciado { get; set; }
        public int? ComercioId { get; set; } // Opcional
    }
}
