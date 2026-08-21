namespace AdLocalAPI.DTOs.UsuarioCliente
{
    public class PerfilClienteDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? FotoUrl { get; set; }
    }

    public class ActualizarPerfilClienteDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? FotoBase64 { get; set; }
    }

    public class PerfilClienteActualizadoDto : PerfilClienteDto
    {
        public string Token { get; set; } = string.Empty;
    }
}
