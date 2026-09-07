using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;

namespace AdLocalAPI.Services
{
    public class UsoCodigoReferidoService : IUsoCodigoReferidoService
    {
        private readonly IUsoCodigoReferidoRepository _repository;
        private readonly JwtContext _jwt;

        public UsoCodigoReferidoService(
            IUsoCodigoReferidoRepository repository,
            JwtContext jwt)
        {
            _repository = repository;
            _jwt = jwt;
        }


        public async Task<ApiResponse<int>> ContarMisUsosAsync()
        {
            long usuarioId = _jwt.GetUserId();

            var total = await _repository.ContarPorReferidorAsync(usuarioId);

            return ApiResponse<int>.Success(total);
        }


        public async Task<ApiResponse<int>> ContarPorCodigoAsync(string codigo)
        {
            var total = await _repository.ContarPorCodigoAsync(codigo);

            return ApiResponse<int>.Success(total);
        }

        public async Task<ApiResponse<int>> ContarTotalAsync()
        {
            var total = await _repository.ContarTotalAsync();

            return ApiResponse<int>.Success(total);
        }
    }
}
