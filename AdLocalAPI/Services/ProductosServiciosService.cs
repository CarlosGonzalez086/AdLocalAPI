using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public class ProductosServiciosService : IProductosServiciosService
    {
        private readonly IProductosServiciosRepository _repository;
        private readonly JwtContext _jwtContext;

        public ProductosServiciosService(IProductosServiciosRepository repository,JwtContext jwtContext)
        {
            _repository = repository;
            _jwtContext = jwtContext;
        }

        public async Task<ApiResponse<ProductosServiciosDto>> CreateAsync(ProductosServiciosDto dto)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                long idComercio = (int)_jwtContext.GetComercioId();
                var entity = new ProductosServicios
                {
                    IdComercio = idComercio,
                    IdUsuario = idUser,
                    Nombre = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    Tipo = (TipoProductoServicio)dto.Tipo,
                    Precio = dto.Precio,
                    Stock = dto.Stock,
                    FechaCreacion = DateTime.UtcNow
                };

                var result = await _repository.CreateAsync(entity);
                dto.Id = result.Id;

                return ApiResponse<ProductosServiciosDto>.Success(dto, "Producto/Servicio creado correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductosServiciosDto>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<IEnumerable<ProductosServiciosDto>>> GetAllAsync(long idComercio)
        {
            var list = await _repository.GetAllAsync(idComercio);
            var result = list.Select(x => new ProductosServiciosDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Tipo = (int)x.Tipo,
                Precio = x.Precio,
                Stock = x.Stock,
                Activo = x.Activo
            });

            return ApiResponse<IEnumerable<ProductosServiciosDto>>
                .Success(result, "Listado obtenido correctamente");
        }

        public async Task<ApiResponse<ProductosServiciosDto>> GetByIdAsync(long id)
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = (int)_jwtContext.GetComercioId();
            var entity = await _repository.GetByIdAsync(id,idComercio,idUser);

            if (entity == null)
                return ApiResponse<ProductosServiciosDto>.Error("404", "Producto/Servicio no encontrado");

            var dto = new ProductosServiciosDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Tipo = (int)entity.Tipo,
                Precio = entity.Precio,
                Stock = entity.Stock,
                Activo = entity.Activo
            };

            return ApiResponse<ProductosServiciosDto>.Success(dto);
        }

        public async Task<ApiResponse<bool>> UpdateAsync(long id, ProductosServiciosDto dto)
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = (int)_jwtContext.GetComercioId();
            var entity = await _repository.GetByIdAsync(id, idComercio, idUser);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            entity.Nombre = dto.Nombre;
            entity.Descripcion = dto.Descripcion;
            entity.Precio = dto.Precio;
            entity.Stock = dto.Stock;
            entity.Activo = dto.Activo;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, "Actualizado correctamente");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = (int)_jwtContext.GetComercioId();
            var entity = await _repository.GetByIdAsync(id, idComercio, idUser);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            entity.Eliminado = true;
            entity.Activo = false;
            entity.FechaEliminado = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, "Eliminado correctamente");
        }

        public async Task<ApiResponse<bool>> DesactivarAsync(long id)
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = (int)_jwtContext.GetComercioId();
            var entity = await _repository.GetByIdAsync(id, idComercio, idUser);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            entity.Activo = entity.Activo ? false : true;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, "Desactivado correctamente");
        }

        public async Task<ApiResponse<PagedResponse<ProductosServiciosDto>>> GetAllPagedAsync(
            int page = 1, int pageSize = 10, string orderBy = "recent", string search = "")
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = (int)_jwtContext.GetComercioId();
            return await _repository.GetAllPagedAsync(idUser,idComercio, page, pageSize, orderBy, search);
        }
    }
}
