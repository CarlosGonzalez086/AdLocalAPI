using AdLocalAPI.Models;
using AdLocalAPI.Repositories;

namespace AdLocalAPI.Services
{
    public class ComercioVisitaService
    {
        private readonly ComercioVisitaRepository _repository;

        public ComercioVisitaService(ComercioVisitaRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<object>> RegistrarVisita(long comercioId, string? ip)
        {
            try
            {
                var registrada = await _repository.RegistrarVisitaUnica(comercioId, ip);

                if (!registrada)
                {
                    return ApiResponse<object>.Success(
                        null,
                        "Visita ya registrada para este dispositivo"
                    );
                }

                return ApiResponse<object>.Success(null, "Visita registrada");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }


        public async Task<ApiResponse<object>> GetStats(long comercioId)
        {
            try
            {
                var stats = await _repository.GetStats(comercioId);
                return ApiResponse<object>.Success(stats, "Estadísticas obtenidas");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
    }
}
