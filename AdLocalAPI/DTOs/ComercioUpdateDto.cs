using AdLocalAPI.Models;

namespace AdLocalAPI.DTOs
{
    public class ComercioUpdateDto
    {
        public string Nombre { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string LogoBase64 { get; set; } = "";
        public List<string> Imagenes { get; set; } = new();
        public double Lat { get; set; } = 0;
        public double Lng { get; set; } = 0;
        public string ColorPrimario { get; set; } = "";
        public string ColorSecundario { get; set; } = "";
        public bool Activo { get; set; } = false;
        public int EstadoId { get; set; } = 0;
        public int MunicipioId { get; set; } = 0;
        public ICollection<HorarioComercio> Horarios { get; set; } = new List<HorarioComercio>();
    }
}
