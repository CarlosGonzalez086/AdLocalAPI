using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.DTOs.Direcciones
{
    public class DireccionUsuarioDto
    {
        [Required]
        [MaxLength(50)]
        public string Alias { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Calle { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NumeroExterior { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? NumeroInterior { get; set; }

        [Required]
        [MaxLength(150)]
        public string Colonia { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string CodigoPostal { get; set; } = string.Empty;

        [Required]
        public int IdEstado { get; set; }

        [Required]
        public int IdMunicipio { get; set; }

        public decimal? Latitud { get; set; }

        public decimal? Longitud { get; set; }

        [MaxLength(500)]
        public string? Referencias { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        public bool EsPredeterminada { get; set; } = false;
    }
}