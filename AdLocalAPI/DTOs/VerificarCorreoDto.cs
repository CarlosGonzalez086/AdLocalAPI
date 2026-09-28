using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.DTOs
{
    public class VerificarCorreoDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Codigo { get; set; } = string.Empty;
    }

    public class ReenviarVerificacionDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
