using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;

namespace AdLocalAPI.Services
{
    public class CalificacionComentarioService : ICalificacionComentarioService
    {
        private readonly ICalificacionComentarioRepository _repository;
        private readonly IComercioRepository _comercioRepository;
        private readonly JwtContext _jwtContext;

        public CalificacionComentarioService(
            ICalificacionComentarioRepository repository,
            IComercioRepository comercioRepository,
            JwtContext jwtContext)
        {
            _repository = repository;
            _comercioRepository = comercioRepository;
            _jwtContext = jwtContext;
        }

        public async Task<ApiResponse<object>> CrearComentario(CalificacionComentarioCreateDto dto)
        {
            try
            {
                if (!_jwtContext.EstaAutenticado())
                    return ApiResponse<object>.Unauthorized("Debes iniciar sesión para publicar una calificación.");

                if (dto.Calificacion < 1 || dto.Calificacion > 5)
                    return ApiResponse<object>.Error("400", "La calificación debe estar entre 1 y 5");

                if (string.IsNullOrWhiteSpace(dto.Comentario))
                    return ApiResponse<object>.Error("400", "El comentario es obligatorio");

                if (dto.Comentario.Length > 250)
                    return ApiResponse<object>.Error("400", "El comentario no puede exceder 250 caracteres");

                if (string.IsNullOrWhiteSpace(dto.NombrePersona))
                    return ApiResponse<object>.Error("400", "El nombre de la persona es obligatorio");

                var comercio = await _comercioRepository.GetByIdAsync(dto.IdComercio);
                if (comercio == null || !comercio.Activo || !comercio.Visible)
                    return ApiResponse<object>.NotFound("Comercio no encontrado o no disponible.");

                var comentario = new CalificacionComentario
                {
                    Calificacion = dto.Calificacion,
                    Comentario = dto.Comentario.Trim(),
                    IdComercio = dto.IdComercio,
                    NombrePersona = dto.NombrePersona.Trim(),
                    FechaCreacion = DateTime.UtcNow
                };

                var creado = await _repository.CreateAsync(comentario);

                return ApiResponse<object>.Success(creado, "Comentario creado correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> ObtenerComentarios(
            long idComercio,
            int page = 1,
            int pageSize = 10,
            string orderBy = "desc"
        )
        {
            try
            {
                var result = await _repository.GetAllAsync(idComercio, page, pageSize, orderBy);
                return ApiResponse<object>.Success(result, "Comentarios obtenidos correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
    }
}
