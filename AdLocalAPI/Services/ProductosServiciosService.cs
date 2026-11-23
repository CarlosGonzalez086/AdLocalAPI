using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using FluentValidation;

namespace AdLocalAPI.Services
{
    public partial class ProductosServiciosService : IProductosServiciosService
    {
        private readonly IProductosServiciosRepository _repository;
        private readonly IComercioRepository _comercioRepository;
        private readonly JwtContext _jwtContext;
        private readonly SuscripcionRepository _suscripcionRepository;
        private readonly UsuarioRepository _usuarioRepository;
        private readonly IValidator<ProductosServiciosDto> _validator; 

        public ProductosServiciosService(
            IProductosServiciosRepository repository,
            IComercioRepository comercioRepository,
            JwtContext jwtContext,
            SuscripcionRepository suscripcionRepository,
            UsuarioRepository usuarioRepository,
            IValidator<ProductosServiciosDto> validator)
        {
            _repository = repository;
            _comercioRepository = comercioRepository;
            _jwtContext = jwtContext;
            _validator = validator;
            _suscripcionRepository = suscripcionRepository;
            _usuarioRepository = usuarioRepository;
        }

        private async Task<(bool Ok, long UserId, long ComercioId, string Codigo, string Mensaje)> ResolverComercioAutorizadoAsync(
            long comercioSolicitadoId,
            bool requierePlanColaborador)
        {
            if (!_jwtContext.EstaAutenticado())
            {
                return (false, 0, 0, "401", "Usuario no autenticado.");
            }

            var rol = _jwtContext.GetUserRole();
            long idComercio = comercioSolicitadoId == 0 ? _jwtContext.GetComercioId() : comercioSolicitadoId;

            if (idComercio <= 0)
            {
                return (false, 0, 0, "900", "Debes registrar un comercio o negocio antes de realizar esta acción.");
            }

            var comercio = await _comercioRepository.GetByIdAsync(idComercio);
            if (comercio == null)
            {
                return (false, 0, 0, "404", "Comercio no encontrado.");
            }

            if (rol == "Colaborador")
            {
                if (idComercio != _jwtContext.GetComercioId())
                {
                    return (false, 0, 0, "403", "No tienes permiso para acceder a otro comercio.");
                }

                var usuario = await _usuarioRepository.GetByIdComercioAsync(idComercio);
                if (usuario == null)
                {
                    return (false, 0, 0, "404", "No se encontró el comercio asociado al colaborador.");
                }

                if (requierePlanColaborador)
                {
                    var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(usuario.Id);
                    if (planActivo?.Plan == null || planActivo.Plan.Tipo == "BASIC" || planActivo.Plan.Tipo == "FREE")
                    {
                        return (false, 0, 0, "400", "El dueño del negocio necesita actualizar su suscripción para que puedas usar las funciones de colaborador.");
                    }
                }

                return (true, usuario.Id, idComercio, "200", string.Empty);
            }

            if (rol == "Comercio")
            {
                long callerId = _jwtContext.GetUserId();
                if (comercio.IdUsuario != callerId)
                {
                    return (false, 0, 0, "403", "No tienes permiso para acceder o modificar productos de este comercio.");
                }

                return (true, callerId, idComercio, "200", string.Empty);
            }

            return (false, 0, 0, "403", "No tienes permiso para gestionar productos o servicios.");
        }

        public async Task<ApiResponse<IEnumerable<ProductosServiciosDto>>> GetAllAsync(long idComercio)
        {
            var auth = await ResolverComercioAutorizadoAsync(idComercio, requierePlanColaborador: false);
            if (!auth.Ok)
            {
                return ApiResponse<IEnumerable<ProductosServiciosDto>>.Error(auth.Codigo, auth.Mensaje);
            }

            if (!_jwtContext.PermiteCatalogo())
            {
                return ApiResponse<IEnumerable<ProductosServiciosDto>>
                    .Success(
                        Enumerable.Empty<ProductosServiciosDto>(),
                        "Listado obtenido correctamente"
                    );
            }

            var list = await _repository.GetAllAsync(auth.ComercioId);
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
                    Version = x.Version,
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
            var auth = await ResolverComercioAutorizadoAsync(0, requierePlanColaborador: true);
            if (!auth.Ok)
            {
                return ApiResponse<ProductosServiciosDto>.Error(auth.Codigo, auth.Mensaje);
            }

            var entity = await _repository.GetByIdAsync(id, auth.ComercioId, auth.UserId);

            if (entity == null)
                return ApiResponse<ProductosServiciosDto>.Error("404", "Producto/Servicio no encontrado");

            var dto = new ProductosServiciosDto
            {
                Id = entity.Id, Uuid = entity.Uuid, Version = entity.Version, IdComercio = entity.IdComercio,
                Nombre = entity.Nombre, Descripcion = entity.Descripcion, Tipo = (int)entity.Tipo,
                Modalidad = (int)entity.Modalidad, Precio = entity.Precio, PrecioDesde = entity.PrecioDesde,
                ManejaStock = entity.ManejaStock, Stock = entity.Stock, Disponible = entity.Disponible,
                PermiteDomicilio = entity.PermiteDomicilio, PermiteRecoger = entity.PermiteRecoger,
                DuracionMinutos = entity.DuracionMinutos, Activo = entity.Activo, Visible = entity.Visible,
                CodigoInterno = entity.CodigoInterno, ImagenBase64 = entity.LogoUrl
            };

            return ApiResponse<ProductosServiciosDto>.Success(dto);
        }

        public async Task<ApiResponse<PagedResponse<ProductosServiciosDto>>> GetAllPagedAsync(
            int page = 1, int pageSize = 10, string orderBy = "recent", string search = "", long idComercio = 0)
        {
            var auth = await ResolverComercioAutorizadoAsync(idComercio, requierePlanColaborador: false);
            if (!auth.Ok)
            {
                return ApiResponse<PagedResponse<ProductosServiciosDto>>.Error(auth.Codigo, auth.Mensaje);
            }

            int maxProductos = _jwtContext.GetMaxProductos();
            return await _repository.GetAllPagedAsync(auth.UserId, auth.ComercioId, page, pageSize, orderBy, search, maxProductos);
        }
    }
}
