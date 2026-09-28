using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ICalificacionComentarioRepository
    {
        Task<CalificacionComentario> CreateAsync(CalificacionComentario comentario);
        Task<object> GetAllAsync(long idComercio, int page = 1, int pageSize = 10, string orderBy = "desc");
        Task<IEnumerable<CalificacionComentario>> GetCalificacionByComercioAsync(long idComercio);
    }
}
