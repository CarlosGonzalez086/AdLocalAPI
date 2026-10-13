using AdLocalAPI.Models;

namespace AdLocalAPI.DTOs
{
    public class CrearCitaDto
    {
        [System.Text.Json.Serialization.JsonRequired] public string ZonaHoraria { get; set; } = Helpers.CitaOperacion.ZonaHoraria;
        public Guid IdempotencyKey { get; set; }
        public Guid ProductoUuid { get; set; }
        public DateTime FechaInicio { get; set; }
        public string NombrePersona { get; set; } = string.Empty;
        public string? Notas { get; set; }
    }

    public class ActualizarCitaComercioDto
    {
        [System.Text.Json.Serialization.JsonRequired] public long VersionCita { get; set; }
        public EstadoCita Estado { get; set; }
        public string? NombreAtiende { get; set; }
        public string? Motivo { get; set; }
    }

    public class ReprogramarCitaDto
    {
        [System.Text.Json.Serialization.JsonRequired] public string ZonaHoraria { get; set; } = Helpers.CitaOperacion.ZonaHoraria;
        [System.Text.Json.Serialization.JsonRequired] public long VersionCita { get; set; }
        public DateTime FechaInicio { get; set; }
    }

    public class CancelarCitaDto
    {
        [System.Text.Json.Serialization.JsonRequired] public long VersionCita { get; set; }
        public string? Motivo { get; set; }
    }

    public class CitaDto
    {
        public long Version { get; set; }
        public string ZonaHoraria { get; set; } = AdLocalAPI.Helpers.CitaOperacion.ZonaHoraria;
        public List<EstadoCita> AccionesDisponibles { get; set; } = [];
        public bool PuedeGestionarCliente { get; set; }
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
