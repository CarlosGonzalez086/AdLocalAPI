using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.DTOs
{
    public class RenovarTokenDto
    {
        [Required(ErrorMessage = "El token actual es obligatorio.")]
        public string TokenActual { get; set; } = string.Empty;
    }
}
