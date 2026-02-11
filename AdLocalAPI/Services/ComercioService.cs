using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Linq;

namespace AdLocalAPI.Services
{
    public class ComercioService
    {
        private readonly ComercioRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly IRelComercioImagenRepositorio _comercioImagenRepositorio;
        private readonly IHorarioComercioService _horarioComercioService;
        private readonly IProductosServiciosRepository _productosServiciosRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly CalificacionComentarioRepository _calificacionComentarioRepository;
        private readonly AppDbContext _context;
        private readonly SuscripcionRepository _suscripcionRepository;
        private readonly PlanRepository _planRepository;
        private readonly UsuarioRepository _usuarioRepository;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;
        private readonly GeoLocationService _geoService;
        private readonly ITipoComercioRepository _tipoComercioRepo;

        public ComercioService(ComercioRepository repository, JwtContext jwtContext, 
                               IRelComercioImagenRepositorio comercioImagenRepositorio, IHorarioComercioService horarioComercioService, ITipoComercioRepository tipoComercioRepo,
                                           SuscripcionRepository suscripcionRepository, GeoLocationService geoService,
            PlanRepository planRepository,
            UsuarioRepository usuarioRepository,
                               IProductosServiciosRepository productosServiciosRepository, ILocationRepository locationRepository,

                               CalificacionComentarioRepository calificacionComentarioRepository, AppDbContext context, IWebHostEnvironment env, EmailService emailService)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _comercioImagenRepositorio = comercioImagenRepositorio;
            _horarioComercioService = horarioComercioService;
            _productosServiciosRepository = productosServiciosRepository;
            _locationRepository = locationRepository;
            _calificacionComentarioRepository = calificacionComentarioRepository;
            _context = context;
            _suscripcionRepository = suscripcionRepository;
            _planRepository = planRepository;
            _usuarioRepository = usuarioRepository;
            _env = env;
            _emailService = emailService;
            _geoService = geoService;
            _tipoComercioRepo = tipoComercioRepo;
        }

        public async Task<ApiResponse<object>> GetAllComercios(
            string tipo,
            double lat,
            double lng,
            string municipio,
            int page,
            int pageSize,
            string ip
        )
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50;

                if (
                     (
                       (lat == 0 || lng == 0)
                       && !string.IsNullOrEmpty(ip)
                       && tipo == "sugeridos"
                     )
                     && ip != "::1"
                  )

                {
                    var geo = await _geoService.GetLocationByIp(ip);

                    if (geo != null)
                    {
                        lat = geo.Value.lat;
                        lng = geo.Value.lng;

                        if (string.IsNullOrEmpty(municipio))
                            municipio = geo.Value.municipio;
                    }
                }

                var (comercios, total) = await _repository.GetAllAsync(
                    tipo,
                    lat,
                    lng,
                    municipio,
                    page,
                    pageSize,
                    ip
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

                var usuario = await _usuarioRepository.GetByIdComercioAsync(id);
                var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(usuario.Id);
                long idPlan = planActivo.PlanId;
                var plan = await _planRepository.GetByIdLongAsync(idPlan);

                if (comercio == null)
                    return ApiResponse<ComercioMineDto>.Error("404", "Comercio no encontrado");

                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                int MaxFotos = plan.MaxFotos;
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

                var listaHorarios = await _horarioComercioService.ObtenerHorariosPorComercioAsync(id);
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
                int MaxProductos = plan.MaxProductos;
                List<ProductosServicios> productos = new List<ProductosServicios>();
                if (plan.PermiteCatalogo) 
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

                Estado estado = null;
                Municipio municipio = null;
                if (comercio.EstadoId != 0)
                {
                    estado = await _locationRepository.GetStateByIdAsync(comercio.EstadoId);
                    municipio = await _locationRepository.GetMunicipalityByIdAsync(comercio.MunicipioId);
                }

                string badge = "";

                var suscripcionActiva = await _context.Suscripcions
                    .Where(s =>
                        s.UsuarioId == comercio.IdUsuario &&
                        s.IsActive &&
                        !s.IsDeleted &&
                        (s.Plan.Tipo == "PRO" || s.Plan.Tipo == "BUSINESS" || s.Plan.Tipo == "BASIC")
                    )
                    .Select(s => new
                    {
                        s.Plan.Tipo,
                        s.Plan.NivelVisibilidad,
                        s.Plan.TieneBadge,
                        s.Plan.BadgeTexto
                    })
                    .FirstOrDefaultAsync();

                if (suscripcionActiva != null && suscripcionActiva.TieneBadge)
                {
                    if (!string.IsNullOrWhiteSpace(suscripcionActiva.BadgeTexto))
                    {
                        badge = suscripcionActiva.BadgeTexto;
                    }
                    else
                    {
                        badge = suscripcionActiva.Tipo switch
                        {
                            "BUSINESS" => "Premium",
                            "PRO" => "Recomendado",
                            _ => "Esencial"
                        };
                    }
                }


                long tipoComercioId = comercio.TipoComercioId != null ? (long)comercio.TipoComercioId : 0;
                TipoComercio tipoComercio = null;
                if (tipoComercioId != 0)
                {
                     tipoComercio = await _tipoComercioRepo.GetById(tipoComercioId);
                }

                var dto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Descripcion = comercio.Descripcion,
                    Email = comercio.Email,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion.Y,
                    Lng = comercio.Ubicacion.X,
                    ColorPrimario = comercio.ColorPrimario,
                    ColorSecundario = comercio.ColorSecundario,
                    Imagenes = Imagenes,
                    Horarios = Horarios,
                    Productos = productos,
                    EstadoNombre = estado == null ? "" : estado.EstadoNombre,
                    MunicipioNombre = municipio == null ? "" : municipio.MunicipioNombre,
                    EstadoId = estado == null ? 0 : estado.Id,
                    MunicipioId = municipio == null ? 0 : municipio.Id,
                    Calificacion = calificacionPromedio,
                    Badge = badge,
                    TipoComercioId = tipoComercioId,
                    TipoComercio = tipoComercio != null ? tipoComercio.Nombre :"",
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
                Comercio comercio;

                var rol = _jwtContext.GetUserRole();

                if (rol == "Colaborador")
                {
                    var comercioId = _jwtContext.GetComercioId();


                    comercio = await _repository.GetByIdAsync(comercioId);
                }
                else
                {
                    comercio = await _repository.GetComercioByUser(idUser);
                }



                if (comercio == null)
                    return ApiResponse<ComercioMineDto>.Error("404", "Comercio no encontrado");

                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                List<string> Imagenes = new List<string>();
                if (listaImagenes.Count > 0)
                {
                    foreach (var item in listaImagenes)
                    {
                        Imagenes.Add(item.FotoUrl);
                    }
                }

                var listaHorarios = await _horarioComercioService.ObtenerHorariosPorComercioAsync(comercio.Id);
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

                Estado estado = null;
                Municipio municipio = null;
                if (comercio.EstadoId != 0)
                {
                     estado = await _locationRepository.GetStateByIdAsync(comercio.EstadoId);
                     municipio = await _locationRepository.GetMunicipalityByIdAsync(comercio.MunicipioId);
                }

                long tipoComercioId = comercio.TipoComercioId != null ? (long)comercio.TipoComercioId : 0;
                TipoComercio tipoComercio = null;
                if (tipoComercioId != 0)
                {
                    tipoComercio = await _tipoComercioRepo.GetById(tipoComercioId);
                }

                string badge = "";

                var suscripcionActiva = await _context.Suscripcions
                    .Where(s =>
                        s.UsuarioId == comercio.IdUsuario &&
                        s.IsActive &&
                        !s.IsDeleted &&
                        (s.Plan.Tipo == "PRO" || s.Plan.Tipo == "BUSINESS" || s.Plan.Tipo == "BASIC")
                    )
                    .Select(s => new
                    {
                        s.Plan.Tipo,
                        s.Plan.NivelVisibilidad,
                        s.Plan.TieneBadge,
                        s.Plan.BadgeTexto
                    })
                    .FirstOrDefaultAsync();

                if (suscripcionActiva != null && suscripcionActiva.TieneBadge)
                {
                    if (!string.IsNullOrWhiteSpace(suscripcionActiva.BadgeTexto))
                    {
                        badge = suscripcionActiva.BadgeTexto;
                    }
                    else
                    {
                        badge = suscripcionActiva.Tipo switch
                        {
                            "BUSINESS" => "Premium",
                            "PRO" => "Recomendado",
                            _ => "Esencial"
                        };
                    }
                }


                var dto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Descripcion = comercio.Descripcion,
                    Email = comercio.Email,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion.Y,
                    Lng = comercio.Ubicacion.X,
                    ColorPrimario = comercio.ColorPrimario,
                    ColorSecundario = comercio.ColorSecundario,
                    Imagenes = Imagenes,
                    Horarios = Horarios,
                    EstadoNombre = estado == null ? "" : estado.EstadoNombre,
                    MunicipioNombre = municipio == null ? "" : municipio.MunicipioNombre,
                    EstadoId = estado == null ? 0 : comercio.EstadoId,
                    MunicipioId = municipio == null ? 0 : municipio.Id,
                    Calificacion = calificacionPromedio,
                    Badge = badge,
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
        public async Task<ApiResponse<object>> CreateComercio(ComercioCreateDto dto)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                string planTipo = _jwtContext.GetPlanTipo();
                if (planTipo != "PRO" && planTipo != "BUSINESS")
                {
                    var comercioExistente = await _repository.GetComercioByUser(idUser);

                    if (comercioExistente != null)
                    {
                        return ApiResponse<object>.Error(
                            "409",
                            "Ya existe un comercio registrado asociado a este usuario"
                        );
                    }
                }

                if (planTipo == "PRO" || planTipo == "BUSINESS") 
                {
                    int maxNegocios = _jwtContext.GetMaxNegocios();
                    List<Comercio> totalNegocios = await _repository.GetAllComerciosByIdUsuario(idUser);
                    if (maxNegocios == totalNegocios.Count)
                    {
                        return ApiResponse<object>.Error(
                            "409",
                            "Has alcanzado el límite de comercios permitidos por tu plan."
                        );
                    }
                }

                if (string.IsNullOrWhiteSpace(dto.Nombre))
                   return ApiResponse<object>.Error(
                        "400",
                        "El nombre del comercio es obligatorio"
                    );

                if (dto.Lat == 0 || dto.Lng == 0)
                    return ApiResponse<object>.Error(
                        "400",
                        "La ubicación del comercio es obligatoria"
                    );
                if (dto.EstadoId == 0)                
                    return ApiResponse<object>.Error(
                       "400",
                       "El estado del comercio es obligatorrio"
                   );
                if (dto.MunicipioId == 0)                
                    return ApiResponse<object>.Error(
                       "400",
                       "El municipio del comercio es obligatorrio"
                   );               
                if (dto.TipoComercioId == 0)                
                    return ApiResponse<object>.Error(
                       "400",
                       "El tipo del comercio es obligatorrio"
                   );               

                string? logoUrl = null;

                if (!string.IsNullOrWhiteSpace(dto.LogoBase64))
                {
                    string? contentType = TiposImagenPermitidos
                        .FirstOrDefault(x => dto.LogoBase64.StartsWith(x.Value))
                        .Key;

                    if (contentType == null)
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            "Formato de imagen no permitido. Usa JPG, PNG o WEBP"
                        );
                    }

                    string base64Clean = dto.LogoBase64
                        .Replace($"data:{contentType};base64,", string.Empty);

                    byte[] imageBytes = Convert.FromBase64String(base64Clean);

                    logoUrl = await _repository.UploadToSupabaseAsync(
                        imageBytes,
                        (int)idUser,
                        contentType
                    );
                }



                var comercio = new Comercio
                {
                    Nombre = dto.Nombre,
                    Direccion = dto.Direccion,
                    Telefono = dto.Telefono,
                    LogoUrl = logoUrl,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    IdUsuario = idUser,
                    ColorPrimario = dto.ColorPrimario,
                    ColorSecundario = dto.ColorSecundario,
                    Descripcion = dto.Descripcion,      
                    Email = dto.Email,
                    EstadoId = dto.EstadoId,
                    MunicipioId = dto.MunicipioId,
                    TipoComercioId = dto.TipoComercioId,
                    Ubicacion = new NetTopologySuite.Geometries.Point(dto.Lng, dto.Lat)
                    {
                        SRID = 4326
                    }
                    
                };

                var creado = await _repository.CreateAsync(comercio);

                if (dto.Horarios.Count > 0)
                {
                    await _horarioComercioService.CrearHorariosAsync(
                        creado.Id,
                        dto.Horarios.ToList()
                    );
                }

                if (dto.Imagenes?.Count > 0)
                {
                    foreach (var item in dto.Imagenes)
                    {
                        try
                        {
                            string? contentType = TiposImagenPermitidos
                                .FirstOrDefault(x => item.StartsWith(x.Value))
                                .Key;

                            if (contentType == null)
                                continue;

                            string base64Clean = item
                                .Replace($"data:{contentType};base64,", string.Empty);

                            byte[] imageBytes = Convert.FromBase64String(base64Clean);

                            var imagenUrl = await _comercioImagenRepositorio
                                .UploadToSupabaseAsync(imageBytes, (int)idUser, contentType);

                            if (string.IsNullOrEmpty(imagenUrl))
                                continue;

                            await _comercioImagenRepositorio.Crear(creado.Id, imagenUrl);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }


                return ApiResponse<object>.Success(
                    null,
                    "El comercio se creó correctamente"
                );
            }
            catch (FormatException)
            {
                return ApiResponse<object>.Error(
                    "400",
                    "La imagen enviada no tiene un formato Base64 válido"
                );
            }
            catch
            {
                return ApiResponse<object>.Error(
                    "500",
                    "Ocurrió un error al crear el comercio"
                );
            }
        }
        public async Task<ApiResponse<object>> UpdateComercio(ComercioUpdateDto dto)
        {
            try
            {
                long idComercio = dto.Id == 0 ? _jwtContext.GetComercioId() : dto.Id;
                var comercio = await _repository.GetByIdAsync(idComercio);

                if (comercio == null)
                    return ApiResponse<object>.Error(
                        "404",
                        "Comercio no encontrado"
                    );
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return ApiResponse<object>.Error(
                        "400",
                        "El nombre del comercio es obligatorio"
                    );

                if (dto.Lat == 0 || dto.Lng == 0)
                    return ApiResponse<object>.Error(
                        "400",
                        "La ubicación del comercio es obligatoria"
                    );
                if (dto.EstadoId == 0)
                    return ApiResponse<object>.Error(
                       "400",
                       "El estado del comercio es obligatorrio"
                   );
                if (dto.MunicipioId == 0)
                    return ApiResponse<object>.Error(
                       "400",
                       "El municipio del comercio es obligatorrio"
                   );
                if (dto.TipoComercioId == 0)
                    return ApiResponse<object>.Error(
                       "400",
                       "El tipo del comercio es obligatorrio"
                   );
                long userId = 0;
                if (_jwtContext.GetUserRole() == "Colaborador")
                {
                    var usuario = await _usuarioRepository.GetByIdComercioAsync(_jwtContext.GetComercioId());                    
                    userId = usuario.Id;
                    var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(userId);
                    
                    if (planActivo.Plan.Tipo == "BASIC" || planActivo.Plan.Tipo == "FREE")
                    {
                        return ApiResponse<object>.Error(
                           "400",
                           "El dueño del negocio necesita actualizar su suscripción para que puedas usar las funciones de colaborador."
                       );

                    }
                }
                else 
                {
                    userId = _jwtContext.GetUserId();
                }
                    


                if (comercio.IdUsuario != userId)
                    return ApiResponse<object>.Error(
                        "403",
                        "No tienes permiso para modificar este comercio"
                    );
                comercio.EstadoId = dto.EstadoId;
                comercio.MunicipioId = dto.MunicipioId;
                comercio.Nombre = dto.Nombre;
                comercio.Direccion = dto.Direccion;
                comercio.Telefono = dto.Telefono;
                comercio.Descripcion = dto.Descripcion;
                comercio.ColorPrimario = dto.ColorPrimario;
                comercio.ColorSecundario = dto.ColorSecundario;
                comercio.Email = dto.Email;
                comercio.Activo = dto.Activo;
                comercio.TipoComercioId = dto.TipoComercioId;

                if (!string.IsNullOrWhiteSpace(dto.LogoBase64) &&
                    !EsUrl(dto.LogoBase64))
                {
                    if (!EsImagenBase64(dto.LogoBase64))
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            "Formato de imagen inválido"
                        );
                    }

                    string? contentType = TiposImagenPermitidos
                        .FirstOrDefault(x => dto.LogoBase64.StartsWith(x.Value))
                        .Key;

                    if (contentType == null)
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            "Formato de imagen no permitido. Usa JPG, PNG o WEBP"
                        );
                    }

                    string base64Clean = dto.LogoBase64.Replace(
                        $"data:{contentType};base64,", string.Empty
                    );

                    byte[] imageBytes = Convert.FromBase64String(base64Clean);

                    if (!string.IsNullOrWhiteSpace(comercio.LogoUrl))
                    {
                        await _repository.DeleteFromSupabaseByUrlAsync(comercio.LogoUrl);
                    }

                    comercio.LogoUrl = await _repository.UploadToSupabaseAsync(
                        imageBytes,
                        (int)userId,
                        contentType
                    );
                }


                if (
                    !double.IsNaN(dto.Lat) &&
                    !double.IsNaN(dto.Lng) &&
                    !double.IsInfinity(dto.Lat) &&
                    !double.IsInfinity(dto.Lng)
                )
                {
                    comercio.Ubicacion = new NetTopologySuite.Geometries.Point(
                        dto.Lng,
                        dto.Lat
                    )
                    {
                        SRID = 4326
                    };
                }
                else
                {
                    comercio.Ubicacion = null;
                }


                await _repository.UpdateAsync(comercio);

                if (dto.Horarios.Count > 0)
                {
                    bool siTiene = await _horarioComercioService.ComercioTieneHorariosAsync(comercio.Id);
                    if (siTiene)
                    {
                        await _horarioComercioService.ActualizarHorariosAsync(comercio.Id,dto.Horarios.ToList());
                    }
                    else 
                    {                      
                        await _horarioComercioService.CrearHorariosAsync(comercio.Id,dto.Horarios.ToList());
                    }
                }

                if (dto.Imagenes?.Count > 0)
                {
                    var imagenesActuales = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                    var urlsRecibidas = dto.Imagenes.Where(x => !string.IsNullOrWhiteSpace(x) && EsUrl(x)).ToList();
                    foreach (var img in imagenesActuales)
                    {
                        if (!urlsRecibidas.Contains(img.FotoUrl))
                        {
                            await _comercioImagenRepositorio.DeleteFromSupabaseByUrlAsync(img.FotoUrl);
                            await _comercioImagenRepositorio.Eliminar(comercio.Id, img.FotoUrl);
                        }
                    }

                    foreach (var item in dto.Imagenes)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(item) &&!EsUrl(item)) 
                            {
                                string? contentType = TiposImagenPermitidos.FirstOrDefault(x => item.StartsWith(x.Value)).Key;

                                if (contentType == null)
                                    continue;

                                string base64Clean = item
                                    .Replace($"data:{contentType};base64,", string.Empty);

                                byte[] imageBytes = Convert.FromBase64String(base64Clean);

                                var imagenUrl = await _comercioImagenRepositorio
                                    .UploadToSupabaseAsync(imageBytes, (int)userId, contentType);

                                if (string.IsNullOrEmpty(imagenUrl))
                                    continue;

                                await _comercioImagenRepositorio
                                    .Crear(comercio.Id, imagenUrl);
                            }
  
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                return ApiResponse<object>.Success(
                    null,
                    "El comercio se actualizó correctamente"
                );
            }
            catch (FormatException)
            {
                return ApiResponse<object>.Error(
                    "400",
                    "La imagen enviada no tiene un formato Base64 válido"
                );
            }
            catch
            {
                return ApiResponse<object>.Error(
                    "500",
                    "Ocurrió un error al actualizar el comercio"
                );
            }
        }
        public async Task<ApiResponse<object>> DeleteComercio(long id)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error(
                        "404",
                        "Comercio no encontrado"
                    );

                long userId = _jwtContext.GetUserId();
                if (comercio.IdUsuario != userId)
                {
                    return ApiResponse<object>.Error(
                        "403",
                        "No tienes permiso para eliminar este comercio"
                    );
                }

                if (!string.IsNullOrWhiteSpace(comercio.LogoUrl))
                {
                    await _repository.DeleteFromSupabaseByUrlAsync(comercio.LogoUrl);
                }
                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                if (listaImagenes.Count > 0)
                {
                    foreach (var img in listaImagenes)
                    {
                        await _comercioImagenRepositorio
                            .Eliminar(comercio.Id, img.FotoUrl);
                    }
                }
                bool siTiene = await _horarioComercioService.ComercioTieneHorariosAsync(comercio.Id);
                if (siTiene)
                {
                    await _horarioComercioService.EliminarHorariosPorComercioAsync(comercio.Id);
                }

                await _repository.DeleteAsync(id);

                return ApiResponse<object>.Success(
                    null,
                    "El comercio se eliminó correctamente"
                );
            }
            catch
            {
                return ApiResponse<object>.Error(
                    "500",
                    "Ocurrió un error al eliminar el comercio"
                );
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
        public async Task<ApiResponse<object>> guardarColaborador(ColaborarDto dto) 
        {
            long idUser = _jwtContext.GetUserId();
            var plan = await _planRepository.GetByTipoAsync(_jwtContext.GetPlanTipo());
            if (plan.IsMultiUsuario)
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return ApiResponse<object>.Error(
                        "400",
                        "El nombre del colaborador es obligatorio"
                    );
                if (string.IsNullOrWhiteSpace(dto.Correo))
                    return ApiResponse<object>.Error(
                        "400",
                        "El correo del colaborador es obligatorio"
                    );
                if (dto.IdComercio == 0)
                    return ApiResponse<object>.Error(
                        "400",
                        "El comercio es obligatorio"
                    );
                bool existente = await _usuarioRepository.ExistePorCorreoAsync(dto.Correo);

                if (existente)
                    return ApiResponse<object>.Error("400", "El correo ya está registrado");

                string codigo = ServicesGenerals.GenerarCodigoAlfanumerico(6);
                string token = Guid.NewGuid().ToString();
                var userColaborador = new Usuario
                {
                    Nombre = dto.Nombre,
                    Email = dto.Correo,
                    ComercioId = dto.IdComercio,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    Codigo = codigo,
                    Token = token,
                    Rol = "Colaborador",
                    PasswordHash = "",
                };

                await _usuarioRepository.CreateAsync(userColaborador);
                bool esProduccion = _env.IsProduction();
                var link = UrlHelper.GenerarLinkNuevoColaborador(token, esProduccion);

                var html = TemplatesEmail.PlantillaCorreoBienvenidaColaborador(dto.Nombre,dto.Correo,codigo, link);

                await _emailService.EnviarCorreoAsync(
                    dto.Correo,
                    "¡Bienvenido a AdLocal! Crea tu contraseña",
                    html
                );
                return ApiResponse<object>.Success(
   null,
    "Colaborador creado correctamente"
);


            }
            return ApiResponse<object>.Error("900", "No tienes permiso para agregar colaborasodree");

        }
        public async Task<ApiResponse<PagedResponse<UsuarioInfoDto>>> getAllColaboradores(long idComercio, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _repository.getAllColaboradores(
                    idComercio,
                    page,
                    pageSize
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<UsuarioInfoDto>>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<bool>> toggleAccesoColaborador(int idColaborador, long idComercio)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var user = await _usuarioRepository.GetByIdComercioAndIdUserAsync(idColaborador, idComercio);
                user.Activo = !user.Activo;
                await _usuarioRepository.UpdateAsync(user);
                return ApiResponse<bool>.Success(true, user.Activo ? "Activado correctamente" : "Desactivado correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<bool>> eliminarColaborador(int idColaborador, long idComercio)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var user = await _usuarioRepository.GetByIdComercioAndIdUserAsync(idColaborador, idComercio);
                if (!string.IsNullOrEmpty(user.FotoUrl))
                {
                    await _usuarioRepository.DeleteFromSupabaseByUrlAsync(user.FotoUrl);
                }
                await _usuarioRepository.DeleteColaboradorAsync(idColaborador,idComercio);
                return ApiResponse<bool>.Success(true, user.Activo ? "Activado correctamente" : "Desactivado correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", ex.Message);
            }
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
