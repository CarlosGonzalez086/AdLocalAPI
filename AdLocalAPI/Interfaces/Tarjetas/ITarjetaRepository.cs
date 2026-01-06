using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Tarjetas
{
    public interface ITarjetaRepository
    {
        Task<List<Tarjeta>> GetByUser(long userId);
        Task<Tarjeta?> GetById(long id, long userId);
        Task Add(Tarjeta tarjeta);
        Task Update(Tarjeta tarjeta);
        Task RemoveDefaults(long userId);
        Task Save();
    }
}
