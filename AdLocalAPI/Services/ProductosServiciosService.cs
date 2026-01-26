using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using FluentValidation;
using Supabase.Gotrue;

namespace AdLocalAPI.Services
{
    public class ProductosServiciosService : IProductosServiciosService
    {
        private readonly IProductosServiciosRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly IValidator<ProductosServiciosDto> _validator; 

        public ProductosServiciosService(
            IProductosServiciosRepository repository,
            JwtContext jwtContext,
            IValidator<ProductosServiciosDto> validator)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _validator = validator;
        }


        public async Task<ApiResponse<ProductosServiciosDto>> CreateAsync(ProductosServiciosDto dto)
        {
            long idUser = _jwtContext.GetUserId();
            int maxProductos = _jwtContext.GetMaxProductos();
            long idComercio = dto.IdComercio == 0 ? _jwtContext.GetComercioId() : dto.IdComercio;
            // Validación aquí
            var validationResult = await _validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                return ApiResponse<ProductosServiciosDto>.Error("400", "Validación fallida");
            }

            try
            {

                if (idComercio == 0 || idComercio == null)
                {
                    return ApiResponse<ProductosServiciosDto>.Error(
                        "900",
                        "Debes registrar un comercio o negocio antes de agregar un producto o servicio."
                    );
                }

                var list = await _repository.GetAllAsync(idComercio);

                if (list.Count() == maxProductos )
                {
                    return ApiResponse<ProductosServiciosDto>.Error(
                        "900",
                        "Has alcanzado el límite de productos o servicios permitidos por tu plan."
                    );
                }


                string? logoUrl = null;
                if (!string.IsNullOrWhiteSpace(dto.ImagenBase64))
                {
                    string? contentType = TiposImagenPermitidos
                        .FirstOrDefault(x => dto.ImagenBase64.StartsWith(x.Value))
                        .Key;

                    if (contentType == null)
                    {
                        return ApiResponse<ProductosServiciosDto>.Error(
                            "400",
                            "Formato de imagen no permitido. Usa JPG, PNG o WEBP"
                        );
                    }

                    string base64Clean = dto.ImagenBase64
                        .Replace($"data:{contentType};base64,", string.Empty);

                    byte[] imageBytes = Convert.FromBase64String(base64Clean);

                    logoUrl = await _repository.UploadToSupabaseAsync(
                        imageBytes,
                        (int)idUser,
                        contentType
                    );
                }
                var entity = new ProductosServicios
                {
                    IdComercio = idComercio,
                    IdUsuario = idUser,
                    Nombre = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    Tipo = (TipoProductoServicio)dto.Tipo,
                    Precio = dto.Precio,
                    Stock = dto.Stock,
                    FechaCreacion = DateTime.UtcNow,
                    LogoUrl = logoUrl,
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
                Stock = (int)x.Stock,
                Activo = x.Activo,
                ImagenBase64 = x.LogoUrl,
            });

            return ApiResponse<IEnumerable<ProductosServiciosDto>>
                .Success(result, "Listado obtenido correctamente");
        }

        public async Task<ApiResponse<ProductosServiciosDto>> GetByIdAsync(long id)
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = _jwtContext.GetComercioId();
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
                Stock = (int)entity.Stock,
                Activo = entity.Activo
            };

            return ApiResponse<ProductosServiciosDto>.Success(dto);
        }

        public async Task<ApiResponse<bool>> UpdateAsync(long id, ProductosServiciosDto dto)
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = dto.IdComercio == 0 ? _jwtContext.GetComercioId() : dto.IdComercio;
            var entity = await _repository.GetByIdAsync(id, idComercio, idUser);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            if (!string.IsNullOrWhiteSpace(dto.ImagenBase64) && !EsUrl(dto.ImagenBase64))
            {
                if (!EsImagenBase64(dto.ImagenBase64))
                {
                    return ApiResponse<bool>.Error(
                        "400",
                        "Formato de imagen inválido"
                    );
                }

                string? contentType = TiposImagenPermitidos
                    .FirstOrDefault(x => dto.ImagenBase64.StartsWith(x.Value))
                    .Key;

                if (contentType == null)
                {
                    return ApiResponse<bool>.Error(
                        "400",
                        "Formato de imagen no permitido. Usa JPG, PNG o WEBP"
                    );
                }

                string base64Clean = dto.ImagenBase64.Replace(
                    $"data:{contentType};base64,", string.Empty
                );

                byte[] imageBytes = Convert.FromBase64String(base64Clean);

                if (!string.IsNullOrWhiteSpace(entity.LogoUrl))
                {
                    await _repository.DeleteFromSupabaseByUrlAsync(entity.LogoUrl);
                }

                entity.LogoUrl = await _repository.UploadToSupabaseAsync(
                    imageBytes,
                    (int)idUser,
                    contentType
                );
            }

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
            long idComercio = _jwtContext.GetComercioId();
            var entity = await _repository.GetByIdAsync(id, idComercio, idUser);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            if (!string.IsNullOrWhiteSpace(entity.LogoUrl))
            {
                await _repository.DeleteFromSupabaseByUrlAsync(entity.LogoUrl);
            }

            entity.Eliminado = true;
            entity.Activo = false;
            entity.FechaEliminado = DateTime.UtcNow;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, "Eliminado correctamente");
        }

        public async Task<ApiResponse<bool>> DesactivarAsync(long id)
        {
            long idUser = _jwtContext.GetUserId();
            long idComercio = _jwtContext.GetComercioId();
            var entity = await _repository.GetByIdAsync(id, idComercio, idUser);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            entity.Activo = entity.Activo ? false : true;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, entity.Activo ? "Activado correctamente" : "Desactivado correctamente");
        }

        public async Task<ApiResponse<PagedResponse<ProductosServiciosDto>>> GetAllPagedAsync(
            int page = 1, int pageSize = 10, string orderBy = "recent", string search = "",long idComercio = 0)
        {
            long idUser = _jwtContext.GetUserId();
            idComercio = idComercio == 0 ? _jwtContext.GetComercioId() : idComercio;
            return await _repository.GetAllPagedAsync(idUser,idComercio, page, pageSize, orderBy, search);
        }
        private static readonly Dictionary<string, string> TiposImagenPermitidos = new()
        {
            { "image/jpeg", "data:image/jpeg;base64," },
            { "image/jpg",  "data:image/jpg;base64,"  },
            { "image/png",  "data:image/png;base64,"  },
            { "image/webp", "data:image/webp;base64," }
        };
        private bool EsUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps;
        }
        private bool EsImagenBase64(string value)
        {
            return value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
