namespace AdLocalAPI.DTOs
{
    public class NotificacionDto
    {
        public Guid Uuid { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public int TipoNotificacion { get; set; }
        public Guid? PedidoUuid { get; set; }
        public string? Url { get; set; }
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class ResumenNotificacionesDto
    {
        public int NoLeidas { get; set; }
        public List<NotificacionDto> Notificaciones { get; set; } = new();
    }
}
