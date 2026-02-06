using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public class TipoComercioService : ITipoComercioService
    {
        private readonly ITipoComercioRepository _repository;

        public TipoComercioService(ITipoComercioRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<object>> Crear(TipoComercioCreateDto dto)
        {
            var entity = new TipoComercio
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Activo = dto.Activo
            };

            await _repository.Create(entity);

            return ApiResponse<object>.Success(
                null,
                "El tipo comercio se creó correctamente"
            );
        }

        public async Task<ApiResponse<object>> Actualizar(long id, TipoComercioCreateDto dto)
        {
            var entity = await _repository.GetById(id);
            if (entity == null)
                return ApiResponse<object>.Error("404", "Tipo de comercio no encontrado");

            entity.Nombre = dto.Nombre;
            entity.Descripcion = dto.Descripcion;
            entity.Activo = dto.Activo;

            await _repository.Update(entity);

            return ApiResponse<object>.Success(
                null,
                "El tipo comercio se creó correctamente"
            );
        }

        public async Task<ApiResponse<bool>> Eliminar(long id)
        {
            var entity = await _repository.GetById(id);
            if (entity == null)
                return ApiResponse<bool>.Error("404", "Tipo de comercio no encontrado");

            await _repository.Delete(entity);
            return ApiResponse<bool>.Success(true, "Eliminado correctamente");
        }

        public async Task<ApiResponse<TipoComercioDto>> GetById(long id)
        {
            var entity = await _repository.GetById(id);
            if (entity == null)
                return ApiResponse<TipoComercioDto>.Error("404", "Tipo de comercio no encontrado");

            return ApiResponse<TipoComercioDto>.Success(new TipoComercioDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Activo = entity.Activo
            });
        }

        public async Task<ApiResponse<PagedResponse<TipoComercioDto>>> GetAllPagedAsync(
            int page = 1,
            int pageSize = 10,
            string orderBy = "recent",
            string search = ""
        )
        {
            return await _repository.GetAllPagedAsync(page, pageSize, orderBy, search);
        }
        public async Task<ApiResponse<List<TipoComercioDto>>> GetAllForSelectAsync()
        {
            var list = await _repository.GetAllForSelect();
            var result = list
                .Select(x => new TipoComercioDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre
                })
                .ToList();

            return ApiResponse<List<TipoComercioDto>>.Success(result);
        }


    }
}
