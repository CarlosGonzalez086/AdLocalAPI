namespace AdLocalAPI.DTOs.Direcciones
{
    public class DireccionUsuarioResponseDto
    {
        public Guid Uuid { get; set; }

        public string Alias { get; set; } = string.Empty;

        public string Calle { get; set; } = string.Empty;

        public string NumeroExterior { get; set; } = string.Empty;

        public string? NumeroInterior { get; set; }

        public string Colonia { get; set; } = string.Empty;

        public string CodigoPostal { get; set; } = string.Empty;

        public int IdEstado { get; set; }

        public string Estado { get; set; } = string.Empty;

        public int IdMunicipio { get; set; }

        public string Municipio { get; set; } = string.Empty;

        public decimal? Latitud { get; set; }

        public decimal? Longitud { get; set; }

        public string? Referencias { get; set; }

        public string? Telefono { get; set; }

        public bool EsPredeterminada { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }
}