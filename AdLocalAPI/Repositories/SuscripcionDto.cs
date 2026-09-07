namespace AdLocalAPI.Repositories
{
    internal class SuscripcionDto
    {
        public long UsuarioId { get; set; }
        public string BadgeTexto { get; set; } = string.Empty;
        public int NivelVisibilidad { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int MaxNegocios { get; set; }
    }
}