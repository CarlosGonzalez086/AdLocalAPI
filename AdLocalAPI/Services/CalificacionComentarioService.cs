using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;

namespace AdLocalAPI.Services
{
    public class CalificacionComentarioService
    {
        private readonly CalificacionComentarioRepository _repository;

        public CalificacionComentarioService(CalificacionComentarioRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<object>> CrearComentario(CalificacionComentarioCreateDto dto)
        {
            try
            {
                if (dto.Calificacion < 1 || dto.Calificacion > 5)
                    return ApiResponse<object>.Error("400", "La calificación debe estar entre 1 y 5");

                if (string.IsNullOrWhiteSpace(dto.Comentario))
                    return ApiResponse<object>.Error("400", "El comentario es obligatorio");

                if (dto.Comentario.Length > 250)
                    return ApiResponse<object>.Error("400", "El comentario no puede exceder 250 caracteres");

                if (string.IsNullOrWhiteSpace(dto.NombrePersona))
                    return ApiResponse<object>.Error("400", "El nombre de la persona es obligatorio");

                var comentario = new CalificacionComentario
                {
                    Calificacion = dto.Calificacion,
                    Comentario = dto.Comentario,
                    IdComercio = dto.IdComercio,
                    NombrePersona = dto.NombrePersona,
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
