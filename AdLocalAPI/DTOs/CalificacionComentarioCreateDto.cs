namespace AdLocalAPI.DTOs
{
    public class CalificacionComentarioCreateDto
    {
        public int Calificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public long IdComercio { get; set; }
        public string NombrePersona { get; set; } = string.Empty;
    }
}
