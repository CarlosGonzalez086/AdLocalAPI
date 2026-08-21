using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IClienteRepository
    {
        Task<Usuario?> ObtenerPorEmailAsync(string email);
        Task<Usuario?> ObtenerPorIdAsync(long id);
        Task<bool> ExisteEmailAsync(string email);
        Task<Usuario> CrearAsync(Usuario usuario);
        Task ActualizarAsync(Usuario usuario);
    }
}