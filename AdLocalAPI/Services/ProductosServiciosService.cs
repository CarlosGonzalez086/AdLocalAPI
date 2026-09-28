using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using FluentValidation;

namespace AdLocalAPI.Services
{
    public partial class ProductosServiciosService : IProductosServiciosService
    {
        private readonly IProductosServiciosRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly SuscripcionRepository _suscripcionRepository;
        private readonly UsuarioRepository _usuarioRepository;
        private readonly IValidator<ProductosServiciosDto> _validator; 

        public ProductosServiciosService(
            IProductosServiciosRepository repository,
            JwtContext jwtContext,
            SuscripcionRepository suscripcionRepository,
            UsuarioRepository usuarioRepository,
            IValidator<ProductosServiciosDto> validator)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _validator = validator;
            _suscripcionRepository = suscripcionRepository;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<ApiResponse<IEnumerable<ProductosServiciosDto>>> GetAllAsync(long idComercio)
        {
            if (!_jwtContext.PermiteCatalogo())
            {
                return ApiResponse<IEnumerable<ProductosServiciosDto>>
                    .Success(
                        Enumerable.Empty<ProductosServiciosDto>(),
                        "Listado obtenido correctamente"
                    );
            }

            var list = await _repository.GetAllAsync(idComercio);
            int maxProductos = _jwtContext.GetMaxProductos();

            var query = list.Where(x => x.Activo && !x.Eliminado);

            if (maxProductos > 0)
            {
                query = query.Take(maxProductos);
            }

            var result = query
                .Select(x => new ProductosServiciosDto
                {
                    Id = x.Id,
                    Uuid = x.Uuid,
                    Nombre = x.Nombre,
                    Descripcion = x.Descripcion,
                    Tipo = (int)x.Tipo,
                    Modalidad = (int)x.Modalidad,
                    Precio = x.Precio,
                    PrecioDesde = x.PrecioDesde,
                    ManejaStock = x.ManejaStock,
                    Stock = x.Stock,
                    Disponible = x.Disponible,
                    PermiteDomicilio = x.PermiteDomicilio,
                    PermiteRecoger = x.PermiteRecoger,
                    DuracionMinutos = x.DuracionMinutos,
                    Activo = x.Activo,
                    Visible = x.Visible,
                    CodigoInterno = x.CodigoInterno,
                    ImagenBase64 = x.LogoUrl,
                    IdComercio = x.IdComercio
                })
                .ToList();

            return ApiResponse<IEnumerable<ProductosServiciosDto>>.Success(
                result,
                "Listado obtenido correctamente"
            );
        }

        public async Task<ApiResponse<ProductosServiciosDto>> GetByIdAsync(long id)
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

            long idComercio = _jwtContext.GetComercioId();
            var entity = await _repository.GetByIdAsync(id, idComercio, userId);

            if (entity == null)
                return ApiResponse<ProductosServiciosDto>.Error("404", "Producto/Servicio no encontrado");

            var dto = new ProductosServiciosDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Tipo = (int)entity.Tipo,
                Precio = entity.Precio,
                Stock = entity.Stock ?? 0,
                Activo = entity.Activo
            };

            return ApiResponse<ProductosServiciosDto>.Success(dto);
        }

        public async Task<ApiResponse<PagedResponse<ProductosServiciosDto>>> GetAllPagedAsync(
            int page = 1, int pageSize = 10, string orderBy = "recent", string search = "", long idComercio = 0)
        {
            long userId = 0;
            if (_jwtContext.GetUserRole() == "Colaborador")
            {
                var usuario = await _usuarioRepository.GetByIdComercioAsync(_jwtContext.GetComercioId());
                if (usuario == null)
                {
                    return ApiResponse<PagedResponse<ProductosServiciosDto>>.NotFound("No se encontró el comercio asociado al colaborador.");
                }
                userId = usuario.Id;
            }
            else
            {
                userId = _jwtContext.GetUserId();
            }

            int maxProductos = _jwtContext.GetMaxProductos();
            idComercio = idComercio == 0 ? _jwtContext.GetComercioId() : idComercio;
            return await _repository.GetAllPagedAsync(userId, idComercio, page, pageSize, orderBy, search, maxProductos);
        }
    }
}
