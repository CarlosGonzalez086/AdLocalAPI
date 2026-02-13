using AdLocalAPI.Models;
using System.Threading.Tasks;

namespace AdLocalAPI.Interfaces.Comercio
{
    public interface IRelComercioImagenRepositorio
    {
        Task<List<RelComercioImagen>> ObtenerPorComercio(long idComercio);
        Task<RelComercioImagen> Crear(long idComercio, string storageKey);
        Task<bool> Editar(long idComercio, string storageKeyActual, string nuevoStorageKey);
        Task<bool> Eliminar(long idComercio, string storageKey);
        Task<string> UploadImageAsync(byte[] imageBytes, long comercioId, string contentType = "image/png");

    }
}