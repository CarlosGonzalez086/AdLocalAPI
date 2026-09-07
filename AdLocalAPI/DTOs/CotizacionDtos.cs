using System;
using AdLocalAPI.Models;

namespace AdLocalAPI.DTOs
{
    public class CrearCotizacionDto
    {
        public Guid ProductoUuid { get; set; }
        public string Solicitud { get; set; } = string.Empty;
    }

    public class CotizacionItemDto
    {
        public Guid Uuid { get; set; }
        public string Servicio { get; set; } = string.Empty;
        public string Comercio { get; set; } = string.Empty;
        public string Solicitud { get; set; } = string.Empty;
        public string? Respuesta { get; set; }
        public decimal? PrecioPropuesto { get; set; }
        public EstadoCotizacion Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
