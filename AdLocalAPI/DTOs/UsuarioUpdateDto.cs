namespace AdLocalAPI.DTOs
{
    public class UsuarioUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int? ComercioId { get; set; } 
    }
}
