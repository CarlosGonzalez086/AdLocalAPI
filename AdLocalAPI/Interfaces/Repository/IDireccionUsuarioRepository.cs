using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IDireccionUsuarioRepository
    {
        Task<List<DireccionUsuario>> ObtenerTodasAsync(
            long idUsuario
        );

        Task<DireccionUsuario?> ObtenerPorUuidAsync(
            long idUsuario,
            Guid uuid
        );

        Task<bool> ExisteEstadoAsync(
            int idEstado
        );

        Task<bool> ExisteMunicipioAsync(
            int idMunicipio
        );

        Task<DireccionUsuario> CrearAsync(
            DireccionUsuario direccion
        );

        Task ActualizarAsync(
            DireccionUsuario direccion
        );

        Task QuitarPredeterminadasAsync(
            long idUsuario,
            long? exceptoId = null
        );

        Task<DireccionUsuario?> ObtenerPrimeraActivaAsync(
            long idUsuario,
            long? exceptoId = null
        );

        Task<bool> TieneDireccionesActivasAsync(
            long idUsuario
        );
    }
}