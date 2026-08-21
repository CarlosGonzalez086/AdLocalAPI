using AdLocalAPI.Models;

namespace AdLocalAPI.DTOs
{
    public class CrearCitaDto
    {
        public Guid ProductoUuid { get; set; }
        public DateTime FechaInicio { get; set; }
        public string NombrePersona { get; set; } = string.Empty;
        public string? Notas { get; set; }
    }

    public class ActualizarCitaComercioDto
    {
        public EstadoCita Estado { get; set; }
        public string? NombreAtiende { get; set; }
        public string? Motivo { get; set; }
    }

    public class ReprogramarCitaDto
    {
        public DateTime FechaInicio { get; set; }
    }

    public class CitaDto
    {
        public Guid Uuid { get; set; }
        public Guid ProductoUuid { get; set; }
        public string Comercio { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string NombrePersona { get; set; } = string.Empty;
        public string? TelefonoCliente { get; set; }
        public string? NotasCliente { get; set; }
        public string? NombreAtiende { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public EstadoCita Estado { get; set; }
        public string? MotivoCancelacion { get; set; }
    }
}
