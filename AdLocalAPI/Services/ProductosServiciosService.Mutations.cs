using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public partial class ProductosServiciosService
    {
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

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private bool EsImagenBase64(string value)
        {
            return value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ApiResponse<ProductosServiciosDto>> CreateAsync(ProductosServiciosDto dto)
        {
            long userId = 0;

            if (_jwtContext.GetUserRole() == "Colaborador")
            {
                var usuario = await _usuarioRepository.GetByIdComercioAsync(_jwtContext.GetComercioId());
                if (usuario == null)
                {
                    return ApiResponse<ProductosServiciosDto>.Error("404", "No se encontró el comercio asociado al colaborador.");
                }
                userId = usuario.Id;

                var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(userId);
                if (planActivo?.Plan == null || planActivo.Plan.Tipo == "BASIC" || planActivo.Plan.Tipo == "FREE")
                {
                    return ApiResponse<ProductosServiciosDto>.Error(
                        "400",
                        "El dueño del negocio necesita actualizar su suscripción para que puedas usar las funciones de colaborador."
                    );
                }
            }
            else
            {
                userId = _jwtContext.GetUserId();
            }

            int maxProductos = _jwtContext.GetMaxProductos();
            long idComercio = dto.IdComercio == 0 ? _jwtContext.GetComercioId() : dto.IdComercio;

            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return ApiResponse<ProductosServiciosDto>.Error("400", "Validación fallida");
            }

            try
            {
                if (idComercio == 0)
                {
                    return ApiResponse<ProductosServiciosDto>.Error(
                        "900",
                        "Debes registrar un comercio o negocio antes de agregar un producto o servicio."
                    );
                }

                var list = await _repository.GetAllAsync(idComercio);
                if (list.Count() >= maxProductos)
                {
                    return ApiResponse<ProductosServiciosDto>.Error(
                        "900",
                        "Has alcanzado el límite de productos o servicios permitidos por tu plan."
                    );
                }

                if (!Enum.IsDefined(typeof(TipoProductoServicio), dto.Tipo))
                {
                    return ApiResponse<ProductosServiciosDto>.Error("400", "El tipo de producto o servicio no es válido.");
                }

                if (!Enum.IsDefined(typeof(ModalidadProductoServicio), dto.Modalidad))
                {
                    return ApiResponse<ProductosServiciosDto>.Error("400", "La modalidad no es válida.");
                }

                var tipo = (TipoProductoServicio)dto.Tipo;
                var modalidad = (ModalidadProductoServicio)dto.Modalidad;

                if (tipo == TipoProductoServicio.Producto && modalidad != ModalidadProductoServicio.Compra)
                {
                    return ApiResponse<ProductosServiciosDto>.Error("400", "Los productos deben utilizar la modalidad de compra.");
                }

                if (modalidad == ModalidadProductoServicio.Compra && (!dto.Precio.HasValue || dto.Precio.Value < 0))
                {
                    return ApiResponse<ProductosServiciosDto>.Error("400", "Los productos disponibles para compra deben tener un precio válido.");
                }

                if (modalidad == ModalidadProductoServicio.Reservacion)
                {
                    if (!dto.Precio.HasValue || dto.Precio.Value < 0)
                    {
                        return ApiResponse<ProductosServiciosDto>.Error("400", "Los servicios con reservación deben tener un precio.");
                    }

                    if (!dto.DuracionMinutos.HasValue || dto.DuracionMinutos.Value <= 0)
                    {
                        return ApiResponse<ProductosServiciosDto>.Error("400", "Los servicios con reservación deben indicar su duración.");
                    }
                }

                if (modalidad == ModalidadProductoServicio.Cotizacion && tipo != TipoProductoServicio.Servicio)
                {
                    return ApiResponse<ProductosServiciosDto>.Error("400", "La modalidad de cotización solo está disponible para servicios.");
                }

                if (dto.ManejaStock)
                {
                    if (!dto.Stock.HasValue)
                    {
                        return ApiResponse<ProductosServiciosDto>.Error("400", "Debes indicar el stock disponible.");
                    }

                    if (dto.Stock.Value < 0)
                    {
                        return ApiResponse<ProductosServiciosDto>.Error("400", "El stock no puede ser negativo.");
                    }
                }
                else
                {
                    dto.Stock = null;
                }

                string? logoUrl = null;

                if (!string.IsNullOrWhiteSpace(dto.ImagenBase64))
                {
                    string? contentType = TiposImagenPermitidos
                        .FirstOrDefault(x => dto.ImagenBase64.StartsWith(x.Value))
                        .Key;

                    if (contentType == null)
                    {
                        return ApiResponse<ProductosServiciosDto>.Error("400", "Formato de imagen no permitido. Usa JPG, PNG o WEBP");
                    }

                    string base64Clean = dto.ImagenBase64.Replace($"data:{contentType};base64,", string.Empty);
                    byte[] imageBytes = Convert.FromBase64String(base64Clean);
                    logoUrl = await _repository.UploadImageAsync(imageBytes, userId, contentType);
                }

                var entity = new ProductosServicios
                {
                    Uuid = Guid.NewGuid(),
                    IdComercio = idComercio,
                    IdUsuario = userId,
                    Nombre = dto.Nombre.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                    Tipo = tipo,
                    Modalidad = modalidad,
                    Precio = dto.Precio,
                    PrecioDesde = dto.PrecioDesde,
                    ManejaStock = dto.ManejaStock,
                    Stock = dto.ManejaStock ? dto.Stock : null,
                    Disponible = dto.Disponible,
                    PermiteDomicilio = dto.PermiteDomicilio,
                    PermiteRecoger = dto.PermiteRecoger,
                    DuracionMinutos = dto.DuracionMinutos,
                    Activo = dto.Activo,
                    Visible = dto.Visible,
                    CodigoInterno = string.IsNullOrWhiteSpace(dto.CodigoInterno) ? null : dto.CodigoInterno.Trim(),
                    FechaCreacion = DateTime.UtcNow,
                    LogoUrl = logoUrl
                };

                var result = await _repository.CreateAsync(entity);

                dto.Id = result.Id;
                dto.Uuid = result.Uuid;

                return ApiResponse<ProductosServiciosDto>.Success(dto, "Producto/Servicio creado correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductosServiciosDto>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> UpdateAsync(long id, ProductosServiciosDto dto)
        {
            long userId = 0;

            if (_jwtContext.GetUserRole() == "Colaborador")
            {
                var usuario = await _usuarioRepository.GetByIdComercioAsync(_jwtContext.GetComercioId());
                if (usuario == null)
                {
                    return ApiResponse<bool>.Error("404", "No se encontró el comercio asociado al colaborador.");
                }
                userId = usuario.Id;

                var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(userId);
                if (planActivo?.Plan == null || planActivo.Plan.Tipo == "BASIC" || planActivo.Plan.Tipo == "FREE")
                {
                    return ApiResponse<bool>.Error(
                        "400",
                        "El dueño del negocio necesita actualizar su suscripción para que puedas usar las funciones de colaborador."
                    );
                }
            }
            else
            {
                userId = _jwtContext.GetUserId();
            }

            long idComercio = dto.IdComercio == 0 ? _jwtContext.GetComercioId() : dto.IdComercio;
            var entity = await _repository.GetByIdAsync(id, idComercio, userId);

            if (entity == null)
            {
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");
            }

            if (!Enum.IsDefined(typeof(TipoProductoServicio), dto.Tipo))
            {
                return ApiResponse<bool>.Error("400", "El tipo de producto o servicio no es válido.");
            }

            if (!Enum.IsDefined(typeof(ModalidadProductoServicio), dto.Modalidad))
            {
                return ApiResponse<bool>.Error("400", "La modalidad no es válida.");
            }

            var tipo = (TipoProductoServicio)dto.Tipo;
            var modalidad = (ModalidadProductoServicio)dto.Modalidad;

            if (tipo == TipoProductoServicio.Producto && modalidad != ModalidadProductoServicio.Compra)
            {
                return ApiResponse<bool>.Error("400", "Los productos deben utilizar la modalidad de compra.");
            }

            if (modalidad == ModalidadProductoServicio.Compra && (!dto.Precio.HasValue || dto.Precio.Value < 0))
            {
                return ApiResponse<bool>.Error("400", "Los productos o servicios de compra directa deben tener un precio válido.");
            }

            if (modalidad == ModalidadProductoServicio.Reservacion)
            {
                if (!dto.Precio.HasValue || dto.Precio.Value < 0)
                {
                    return ApiResponse<bool>.Error("400", "Los servicios con reservación deben tener un precio.");
                }

                if (!dto.DuracionMinutos.HasValue || dto.DuracionMinutos.Value <= 0)
                {
                    return ApiResponse<bool>.Error("400", "Los servicios con reservación deben indicar su duración.");
                }
            }

            if (modalidad == ModalidadProductoServicio.Cotizacion && tipo != TipoProductoServicio.Servicio)
            {
                return ApiResponse<bool>.Error("400", "La modalidad de cotización solo está disponible para servicios.");
            }

            if (dto.ManejaStock)
            {
                if (!dto.Stock.HasValue)
                {
                    return ApiResponse<bool>.Error("400", "Debes indicar el stock disponible.");
                }

                if (dto.Stock.Value < 0)
                {
                    return ApiResponse<bool>.Error("400", "El stock no puede ser negativo.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.ImagenBase64) && !EsUrl(dto.ImagenBase64))
            {
                if (!EsImagenBase64(dto.ImagenBase64))
                {
                    return ApiResponse<bool>.Error("400", "Formato de imagen inválido");
                }

                string? contentType = TiposImagenPermitidos
                    .FirstOrDefault(x => dto.ImagenBase64.StartsWith(x.Value))
                    .Key;

                if (contentType == null)
                {
                    return ApiResponse<bool>.Error("400", "Formato de imagen no permitido. Usa JPG, PNG o WEBP");
                }

                string base64Clean = dto.ImagenBase64.Replace($"data:{contentType};base64,", string.Empty);
                byte[] imageBytes = Convert.FromBase64String(base64Clean);

                if (!string.IsNullOrWhiteSpace(entity.LogoUrl))
                {
                    await _repository.DeleteFromS3Async(entity.LogoUrl);
                }

                entity.LogoUrl = await _repository.UploadImageAsync(imageBytes, userId, contentType);
            }

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
            entity.Tipo = tipo;
            entity.Modalidad = modalidad;
            entity.Precio = dto.Precio;
            entity.PrecioDesde = dto.PrecioDesde;
            entity.ManejaStock = dto.ManejaStock;
            entity.Stock = dto.ManejaStock ? dto.Stock : null;
            entity.Disponible = dto.Disponible;
            entity.PermiteDomicilio = dto.PermiteDomicilio;
            entity.PermiteRecoger = dto.PermiteRecoger;
            entity.DuracionMinutos = dto.DuracionMinutos;
            entity.Activo = dto.Activo;
            entity.Visible = dto.Visible;
            entity.CodigoInterno = string.IsNullOrWhiteSpace(dto.CodigoInterno) ? null : dto.CodigoInterno.Trim();
            entity.FechaActualizacion = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, "Actualizado correctamente");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id, long idComercio)
        {
            long userId = 0;
            if (_jwtContext.GetUserRole() == "Colaborador")
            {
                var usuario = await _usuarioRepository.GetByIdComercioAsync(_jwtContext.GetComercioId());
                if (usuario == null)
                {
                    return ApiResponse<bool>.Error("404", "No se encontró el comercio asociado al colaborador.");
                }
                userId = usuario.Id;
                var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(userId);
                if (planActivo?.Plan == null || planActivo.Plan.Tipo == "BASIC" || planActivo.Plan.Tipo == "FREE")
                {
                    return ApiResponse<bool>.Error(
                       "400",
                       "El dueño del negocio necesita actualizar su suscripción para que puedas usar las funciones de colaborador."
                   );
                }
            }
            else
            {
                userId = _jwtContext.GetUserId();
            }

            idComercio = idComercio == 0 ? _jwtContext.GetComercioId() : idComercio;
            var entity = await _repository.GetByIdAsync(id, idComercio, userId);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            if (!string.IsNullOrWhiteSpace(entity.LogoUrl))
            {
                await _repository.DeleteFromS3Async(entity.LogoUrl);
            }

            entity.Eliminado = true;
            entity.Activo = false;
            entity.FechaEliminado = DateTime.UtcNow;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, "Eliminado correctamente");
        }

        public async Task<ApiResponse<bool>> DesactivarAsync(long id, long idComercio)
        {
            long userId = 0;
            if (_jwtContext.GetUserRole() == "Colaborador")
            {
                var usuario = await _usuarioRepository.GetByIdComercioAsync(_jwtContext.GetComercioId());
                if (usuario == null)
                {
                    return ApiResponse<bool>.Error("404", "No se encontró el comercio asociado al colaborador.");
                }
                userId = usuario.Id;
                var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(userId);
                if (planActivo?.Plan == null || planActivo.Plan.Tipo == "BASIC" || planActivo.Plan.Tipo == "FREE")
                {
                    return ApiResponse<bool>.Error(
                       "400",
                       "El dueño del negocio necesita actualizar su suscripción para que puedas usar las funciones de colaborador."
                   );
                }
            }
            else
            {
                userId = _jwtContext.GetUserId();
            }

            idComercio = idComercio == 0 ? _jwtContext.GetComercioId() : idComercio;
            var entity = await _repository.GetByIdAsync(id, idComercio, userId);

            if (entity == null)
                return ApiResponse<bool>.Error("404", "Producto/Servicio no encontrado");

            entity.Activo = entity.Activo ? false : true;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            return ApiResponse<bool>.Success(true, entity.Activo ? "Activado correctamente" : "Desactivado correctamente");
        }
    }
}
