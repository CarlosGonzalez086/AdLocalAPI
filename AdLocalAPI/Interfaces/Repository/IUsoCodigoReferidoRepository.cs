using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IUsoCodigoReferidoRepository
    {
        Task<bool> InsertarAsync(long usuarioReferidorId, long usuarioReferidoId, string codigoReferido);
        Task<bool> EliminarAsync(long id);
        Task<List<UsoCodigoReferido>> ObtenerPorReferidorAsync(long usuarioReferidorId);
        Task<UsoCodigoReferido?> ObtenerPorUsuarioReferidoAsync(long usuarioReferidoId);
        Task<List<UsoCodigoReferido>> ObtenerTodosAsync();
        Task<int> ContarPorReferidorAsync(long usuarioReferidorId);
        Task<int> ContarPorCodigoAsync(string codigoReferido);
        Task<int> ContarTotalAsync();
    }
}
