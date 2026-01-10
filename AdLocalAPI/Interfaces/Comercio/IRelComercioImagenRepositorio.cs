using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Comercio
{
    public interface IRelComercioImagenRepositorio
    {
        Task<List<RelComercioImagen>> ObtenerPorComercio(long idComercio);
        Task<RelComercioImagen> Crear(long idComercio, string fotoUrl);
        Task<bool> Editar(long idComercio, string fotoUrlActual, string nuevaFotoUrl);
        Task<bool> Eliminar(long idComercio, string fotoUrl);
        Task<string> UploadToSupabaseAsync(byte[] imageBytes, int userId, string contentType = "image/png");
        Task<bool> DeleteFromSupabaseByUrlAsync(string publicUrl);
    }
}
