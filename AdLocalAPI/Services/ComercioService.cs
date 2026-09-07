using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Hosting;

namespace AdLocalAPI.Services
{
    public partial class ComercioService : IComercioService
    {
        private readonly IComercioRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly IRelComercioImagenRepositorio _comercioImagenRepositorio;
        private readonly IHorarioComercioRepository _horarioComercioRepository;
        private readonly IProductosServiciosRepository _productosServiciosRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICalificacionComentarioRepository _calificacionComentarioRepository;
        private readonly ISuscripcionRepository _suscripcionRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly IGeoLocationService _geoService;
        private readonly ITipoComercioRepository _tipoComercioRepo;
        private readonly IRefreshTokenService _refreshTokenService;

        public ComercioService(IComercioRepository repository, JwtContext jwtContext, 
                               IRelComercioImagenRepositorio comercioImagenRepositorio, IHorarioComercioRepository horarioComercioRepository, ITipoComercioRepository tipoComercioRepo,
                                           ISuscripcionRepository suscripcionRepository, IGeoLocationService geoService,
            IPlanRepository planRepository,
            IUsuarioRepository usuarioRepository,
                               IProductosServiciosRepository productosServiciosRepository, ILocationRepository locationRepository,

                               ICalificacionComentarioRepository calificacionComentarioRepository, IWebHostEnvironment env, IEmailService emailService,
                               IRefreshTokenService refreshTokenService)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _comercioImagenRepositorio = comercioImagenRepositorio;
            _horarioComercioRepository = horarioComercioRepository;
            _productosServiciosRepository = productosServiciosRepository;
            _locationRepository = locationRepository;
            _calificacionComentarioRepository = calificacionComentarioRepository;
            _suscripcionRepository = suscripcionRepository;
            _planRepository = planRepository;
            _usuarioRepository = usuarioRepository;
            _env = env;
            _emailService = emailService;
            _geoService = geoService;
            _tipoComercioRepo = tipoComercioRepo;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<ApiResponse<object>> GetAllComercios(
            string tipo,
            double lat,
            double lng,
            string municipio,
            int page,
            int pageSize,
            string ip,
            long? idCliente = null
        )
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50;

                var (comercios, total) = await _repository.GetAllAsync(
                    tipo,
                    lat,
                    lng,
                    municipio,
                    page,
                    pageSize,
                    ip,
                    idCliente
                );

                return ApiResponse<object>.Success(
                    new
                    {
                        items = comercios,
                        total,
                        page,
                        pageSize
                    },
                    "Listado de comercios obtenido correctamente"
                );
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<object>.Error("400", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<ComercioMineDto>> GetComercioById(long id)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);
                if (comercio == null)
                    return ApiResponse<ComercioMineDto>.Error("404", "Comercio no encontrado");

                var usuario = await _usuarioRepository.GetByIdComercioAsync(id);
                var planActivo = usuario != null ? await _suscripcionRepository.GetActivaByUsuarioAsync(usuario.Id) : null;
                var plan = planActivo != null ? await _planRepository.GetByIdLongAsync(planActivo.PlanId) : null;

                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                int MaxFotos = plan?.MaxFotos ?? 5;
                listaImagenes = listaImagenes
                  .Take(MaxFotos)
                  .ToList();
                List<string> Imagenes = new List<string>();
                if (listaImagenes.Count > 0)
                {
                    foreach (var item in listaImagenes)
                    {
                        Imagenes.Add(item.FotoUrl);
                    }
                }

                var listaHorarios = await _horarioComercioRepository.ObtenerHorariosPorComercioAsync(id);
                List<HorariosMineDto> Horarios = new List<HorariosMineDto>();
                if (listaHorarios.Count > 0)
                {
                    foreach (var item in listaHorarios)
                    {
                        var dtoHorario = new HorariosMineDto
                        {
                            Id = item.Id,
                            ComercioId = item.ComercioId,
                            Dia = item.Dia,
                            Abierto = item.Abierto,
                            HoraApertura = item.HoraApertura,
                            HoraCierre = item.HoraCierre
                        };
                        Horarios.Add(dtoHorario);
                    }
                }
                var listaProductos = await _productosServiciosRepository.GetAllAsync(id);
                int MaxProductos = plan?.MaxProductos ?? 10;
                List<ProductosServicios> productos = new List<ProductosServicios>();
                if (plan?.PermiteCatalogo == true) 
                {
                    listaProductos = listaProductos.Take(MaxProductos);
                    listaProductos = listaProductos.Where(c => c.Activo);

                    if (listaProductos.Count() > 0)
                    {
                        foreach (var item in listaProductos)
                        {
                            productos.Add(item);
                        }
                    }
                }

                var listaCalificaciones =
                    await _calificacionComentarioRepository.GetCalificacionByComercioAsync(id);

                double calificacionPromedio = 0;
                double totalCalif = listaCalificaciones.Count();

                if (listaCalificaciones.Any())
                {
                    int sumaCalificaciones = listaCalificaciones.Sum(x => x.Calificacion);
                    calificacionPromedio = (double)sumaCalificaciones / totalCalif;
                }

                Estado? estado = null;
                Municipio? municipio = null;
                if (comercio.EstadoId != 0)
                {
                    estado = await _locationRepository.GetStateByIdAsync(comercio.EstadoId);
                    municipio = await _locationRepository.GetMunicipalityByIdAsync(comercio.MunicipioId);
                }

                string? badge = await _suscripcionRepository.ObtenerBadgeTextoUsuarioAsync(comercio.IdUsuario);

                long tipoComercioId = comercio.TipoComercioId != null ? (long)comercio.TipoComercioId : 0;
                TipoComercio? tipoComercio = null;
                if (tipoComercioId != 0)
                {
                     tipoComercio = await _tipoComercioRepo.GetById(tipoComercioId);
                }

                var dto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion ?? "",
                    Telefono = comercio.Telefono ?? "",
                    Descripcion = comercio.Descripcion ?? "",
                    Email = comercio.Email ?? "",
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl ?? "",
                    Lat = comercio.Ubicacion?.Y ?? 0,
                    Lng = comercio.Ubicacion?.X ?? 0,
                    ColorPrimario = comercio.ColorPrimario ?? "",
                    ColorSecundario = comercio.ColorSecundario ?? "",
                    Imagenes = Imagenes,
                    Horarios = Horarios,
                    Productos = productos,
                    EstadoNombre = estado == null ? "" : estado.EstadoNombre,
                    MunicipioNombre = municipio == null ? "" : municipio.MunicipioNombre,
                    EstadoId = estado == null ? 0 : estado.Id,
                    MunicipioId = municipio == null ? 0 : municipio.Id,
                    Calificacion = calificacionPromedio,
                    Badge = badge ?? "",
                    TipoComercioId = tipoComercioId,
                    TipoComercio = tipoComercio != null ? tipoComercio.Nombre : "",
                };

                return ApiResponse<ComercioMineDto>.Success(
                    dto,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ComercioMineDto>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<ComercioMineDto>> GetComercioByUser()
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                Comercio? comercio;

                var rol = _jwtContext.GetUserRole();
                Suscripcion? planActivo = null;
                if (rol == "Colaborador")
                {
                    var comercioId = _jwtContext.GetComercioId();

                    comercio = await _repository.GetByIdAsync(comercioId);
                    if (comercio != null)
                    {
                        planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(comercio.IdUsuario);
                    }
                }
                else
                {
                    comercio = await _repository.GetComercioByUser(idUser);
                    planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(idUser);
                }

                if (comercio == null)
                    return ApiResponse<ComercioMineDto>.Error("404", "Comercio no encontrado");

                long idPlan = planActivo?.PlanId ?? 0;
                var plan = idPlan != 0 ? await _planRepository.GetByIdLongAsync(idPlan) : null;

                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                int MaxFotos = plan?.MaxFotos ?? 5;
                listaImagenes = listaImagenes
                  .Take(MaxFotos)
                  .ToList();
                List<string> Imagenes = new List<string>();
                if (listaImagenes.Count > 0)
                {
                    foreach (var item in listaImagenes)
                    {
                        Imagenes.Add(item.FotoUrl);
                    }
                }

                var listaHorarios = await _horarioComercioRepository.ObtenerHorariosPorComercioAsync(comercio.Id);
                List<HorariosMineDto> Horarios = new List<HorariosMineDto>();
                if (listaHorarios.Count > 0)
                {
                    foreach (var item in listaHorarios)
                    {
                        var dtoHorario = new HorariosMineDto
                        {
                            Id = item.Id,
                            ComercioId = item.ComercioId,
                            Dia = item.Dia,
                            Abierto = item.Abierto,
                            HoraApertura = item.HoraApertura,
                            HoraCierre = item.HoraCierre
                        };
                        Horarios.Add(dtoHorario);
                    }
                }

                var listaCalificaciones =
                    await _calificacionComentarioRepository.GetCalificacionByComercioAsync(comercio.Id);

                double calificacionPromedio = 0;
                double totalCalif = listaCalificaciones.Count();

                if (listaCalificaciones.Any())
                {
                    int sumaCalificaciones = listaCalificaciones.Sum(x => x.Calificacion);
                    calificacionPromedio = (double)sumaCalificaciones / totalCalif;
                }

                Estado? estado = null;
                Municipio? municipio = null;
                if (comercio.EstadoId != 0)
                {
                     estado = await _locationRepository.GetStateByIdAsync(comercio.EstadoId);
                     municipio = await _locationRepository.GetMunicipalityByIdAsync(comercio.MunicipioId);
                }

                long tipoComercioId = comercio.TipoComercioId != null ? (long)comercio.TipoComercioId : 0;
                TipoComercio? tipoComercio = null;
                if (tipoComercioId != 0)
                {
                    tipoComercio = await _tipoComercioRepo.GetById(tipoComercioId);
                }

                string? badge = await _suscripcionRepository.ObtenerBadgeTextoUsuarioAsync(comercio.IdUsuario);

                var dto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion ?? "",
                    Telefono = comercio.Telefono ?? "",
                    Descripcion = comercio.Descripcion ?? "",
                    Email = comercio.Email ?? "",
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl ?? "",
                    Lat = comercio.Ubicacion?.Y ?? 0,
                    Lng = comercio.Ubicacion?.X ?? 0,
                    ColorPrimario = comercio.ColorPrimario ?? "",
                    ColorSecundario = comercio.ColorSecundario ?? "",
                    Imagenes = Imagenes,
                    Horarios = Horarios,
                    EstadoNombre = estado == null ? "" : estado.EstadoNombre,
                    MunicipioNombre = municipio == null ? "" : municipio.MunicipioNombre,
                    EstadoId = estado == null ? 0 : comercio.EstadoId,
                    MunicipioId = municipio == null ? 0 : municipio.Id,
                    Calificacion = calificacionPromedio,
                    Badge = badge ?? "",
                    TipoComercioId = tipoComercioId,
                    TipoComercio = tipoComercio != null ? tipoComercio.Nombre : "",
                };

                return ApiResponse<ComercioMineDto>.Success(
                    dto,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ComercioMineDto>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> GetByFiltros(
            int estadoId,
            int municipioId,
            long idTipoComercio,
            string orden,
            int page,
            int pageSize
        )
        {
            try
            {
                var (items, total) = await _repository.GetByFiltros(
                    estadoId,
                    municipioId,
                    idTipoComercio,
                    orden,
                    page,
                    pageSize
                );

                return ApiResponse<object>.Success(
                    new
                    {
                        items,
                        total,
                        page,
                        pageSize
                    },
                    "Listado de comercios obtenido correctamente"
                );
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<object>.Error("400", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<PagedResponse<ComercioPublicDto>>> GetAllComerciosByUserPaged(
            int page = 1,
            int pageSize = 10
        )
        {
            try
            {
                long idUser = _jwtContext.GetUserId();

                return await _repository.GetAllComerciosByUserPaged(
                    idUser,
                    page,
                    pageSize
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<ComercioPublicDto>>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> GeTotalComerciosByIdUsuario()
        {
            long idUser = _jwtContext.GetUserId();
            List<Comercio> totalNegocios = await _repository.GetAllComerciosByIdUsuario(idUser);
            return ApiResponse<object>.Success(totalNegocios.Count,"Total de comercios");
        }
    }
}
