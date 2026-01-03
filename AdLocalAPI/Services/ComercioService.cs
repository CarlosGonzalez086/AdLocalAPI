using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;

namespace AdLocalAPI.Services
{
    public class ComercioService
    {
        private readonly ComercioRepository _repository;
        private readonly JwtContext _jwtContext;

        public ComercioService(ComercioRepository repository, JwtContext jwtContext)
        {
            _repository = repository;
            _jwtContext = jwtContext;
        }

        // 🔹 Obtener todos los comercios
        public async Task<ApiResponse<object>> GetAllComercios()
        {
            try
            {
                var comercios = await _repository.GetAllAsync();

                return ApiResponse<object>.Success(
                    comercios,
                    "Listado de comercios obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        // 🔹 Obtener comercio por ID
        public async Task<ApiResponse<object>> GetComercioById(int id)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error("404", "Comercio no encontrado");

                return ApiResponse<object>.Success(
                    comercio,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        // 🔹 Obtener comercio por ID
        public async Task<ApiResponse<object>> GetComercioByUser()
        {
            try
            {
                long idUSer = _jwtContext.GetUserId();
                var comercio = await _repository.GetComercioByUser(idUSer);

                if (comercio == null)
                    return ApiResponse<object>.Error("404", "Comercio no encontrado");

                return ApiResponse<object>.Success(
                    comercio,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        // 🔹 Crear comercio
        public async Task<ApiResponse<object>> CreateComercio(ComercioCreateDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Nombre))
                    return ApiResponse<object>.Error("400", "El nombre del comercio es obligatorio");

                var comercio = new Comercio
                {
                    Nombre = dto.Nombre,
                    Direccion = dto.Direccion,
                    Telefono = dto.Telefono,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    IdUsuario = _jwtContext.GetUserId(),
                };

                var creado = await _repository.CreateAsync(comercio);

                return ApiResponse<object>.Success(
                    creado,
                    "Comercio creado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        // 🔹 Actualizar comercio
        public async Task<ApiResponse<object>> UpdateComercio(int id, ComercioUpdateDto dto)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error("404", "Comercio no encontrado");

                long userId = _jwtContext.GetUserId();

                if (comercio.IdUsuario != userId)
                    return ApiResponse<object>.Error(
                        "403",
                        "No tienes permiso para modificar este comercio"
                    );

                comercio.Nombre = dto.Nombre;
                comercio.Direccion = dto.Direccion;
                comercio.Telefono = dto.Telefono;
                comercio.Activo = dto.Activo;

                await _repository.UpdateAsync(comercio);

                return ApiResponse<object>.Success(
                    comercio,
                    "Comercio actualizado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }

        }

        // 🔹 Eliminar comercio
        public async Task<ApiResponse<object>> DeleteComercio(int id)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error("404", "Comercio no encontrado");

                await _repository.DeleteAsync(id);

                return ApiResponse<object>.Success(
                    null,
                    "Comercio eliminado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
    }
}
