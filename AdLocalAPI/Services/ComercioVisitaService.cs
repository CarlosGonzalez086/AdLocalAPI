using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;

namespace AdLocalAPI.Services
{
    public class ComercioVisitaService : IComercioVisitaService
    {
        private readonly IComercioVisitaRepository _repository;
        private readonly IComercioRepository _comercioRepository;
        private readonly JwtContext _jwtContext;

        public ComercioVisitaService(
            IComercioVisitaRepository repository,
            IComercioRepository comercioRepository,
            JwtContext jwtContext)
        {
            _repository = repository;
            _comercioRepository = comercioRepository;
            _jwtContext = jwtContext;
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
                if (!_jwtContext.EstaAutenticado())
                {
                    return ApiResponse<object>.Error("401", "No autenticado.");
                }

                var comercio = await _comercioRepository.GetByIdAsync(comercioId);
                if (comercio == null)
                {
                    return ApiResponse<object>.Error("404", "Comercio no encontrado.");
                }

                if (!_jwtContext.EsAdministrador())
                {
                    var rol = _jwtContext.GetUserRole();
                    if (rol == "Colaborador")
                    {
                        if (_jwtContext.GetComercioId() != comercioId)
                        {
                            return ApiResponse<object>.Error("403", "No tienes permiso para ver las estadísticas de este comercio.");
                        }
                    }
                    else if (_jwtContext.EsComercio())
                    {
                        if (comercio.IdUsuario != _jwtContext.GetUserId())
                        {
                            return ApiResponse<object>.Error("403", "No tienes permiso para ver las estadísticas de este comercio.");
                        }
                    }
                    else
                    {
                        return ApiResponse<object>.Error("403", "No tienes permiso para consultar estadísticas de comercios.");
                    }
                }

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
