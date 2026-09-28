using System;

namespace AdLocalAPI.DTOs
{
    public class SesionActivaDto
    {
        public long Id { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public string? IpOrigen { get; set; }
        public bool EsSesionActual { get; set; }
    }
}
