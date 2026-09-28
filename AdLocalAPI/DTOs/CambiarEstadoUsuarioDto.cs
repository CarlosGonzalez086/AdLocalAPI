namespace AdLocalAPI.DTOs
{
    public class CambiarEstadoUsuarioDto
    {
        public bool Activo { get; set; }
        public string? Motivo { get; set; }
    }

    public class CambiarRolUsuarioDto
    {
        public string NuevoRol { get; set; } = string.Empty;
    }
}
