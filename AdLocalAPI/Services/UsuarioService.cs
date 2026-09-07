using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdLocalAPI.Services.Interfaces;

using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace AdLocalAPI.Services
{
    public partial class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repository;
        private readonly IConfiguration _config;
        private readonly JwtContext _jwtContext;
        private readonly IComercioRepository _comercioRepository;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly ISuscripcionRepository _suscripcionRepository;
        private readonly IPlanRepository _planRepo;
        private readonly IUsoCodigoReferidoRepository _usoCodigoReferidoRepository;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioService(IUsuarioRepository repository, IConfiguration config, JwtContext jwtContext, IComercioRepository comercioRepository, IEmailService emailService, IWebHostEnvironment env, ISuscripcionRepository suscripcionRepository,
            IPlanRepository planRepo, IUsoCodigoReferidoRepository usoCodigoReferidoRepository, IRefreshTokenService refreshTokenService, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _config = config;
            _jwtContext = jwtContext;
            _comercioRepository = comercioRepository;
            _emailService = emailService;
            _env = env;
            _suscripcionRepository = suscripcionRepository;
            _planRepo = planRepo;
            _usoCodigoReferidoRepository = usoCodigoReferidoRepository;
            _refreshTokenService = refreshTokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<PagedResponse<Models.Usuario>>> GetAllUsuarios(int page,
            int pageSize,
            string orderBy,
            string search)
        {        
                return await _repository.GetAllAsync(page, pageSize, orderBy, search);
        }
        public async Task<ApiResponse<object>> GetUsuarioById(int id)
        {
            try
            {
                var usuario = await _repository.GetByIdAsync(id);

                if (usuario == null)
                    return ApiResponse<object>.Error("404", "Usuario no encontrado");

                return ApiResponse<object>.Success(
                    usuario,
                    "Usuario obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> CrearUsuarioCliente(UsuarioRegistroDto dto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(dto.Nombre))
                return ApiResponse<object>.Error("400", "El nombre es obligatorio");

            if (string.IsNullOrEmpty(dto.Email))
                return ApiResponse<object>.Error("400", "El email es obligatorio");

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            if (!emailRegex.IsMatch(dto.Email))
                return ApiResponse<object>.Error("400", "El correo electrónico no es válido");

            if (string.IsNullOrEmpty(dto.Password))
                return ApiResponse<object>.Error("400", "La contraseña es obligatoria");

            bool existente = await _repository.ExistePorCorreoAsync(dto.Email);

            if (existente)
                return ApiResponse<object>.Error("400", "El correo ya está registrado");
            string codigoRef = string.Empty;
            const int MAX_INTENTOS = 5;

            for (int i = 0; i < MAX_INTENTOS; i++)
            {
                codigoRef = CodigoReferidoGenerator.Generar();

                var existe = await _repository.GetByCodigoReferidoAsync(codigoRef);
                if (existe == null)
                    break;
            }

            if (string.IsNullOrEmpty(codigoRef))
            {
                return ApiResponse<object>.Error(
                    "500",
                    "No se pudo generar un código de referido único"
                );
            }


            var codigoVerificacion = ServicesGenerals.GenerarCodigoAlfanumerico(6);

            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                Rol = "Comercio",
                ComercioId = dto.ComercioId,
                CodigoReferido = codigoRef,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FechaCreacion = DateTime.UtcNow,
                EmailVerificado = false,
                Codigo = codigoVerificacion,
                CodigoExpiracion = DateTime.UtcNow.AddHours(24),
                IntentosCodigo = 0
            };

            var creado = await _repository.CreateAsync(usuario);

            if (!string.IsNullOrEmpty(dto.CodigoReferenciado))
            {
                var usuarioReferido = await _repository.GetByCodigoReferidoAsync(dto.CodigoReferenciado);
                if (usuarioReferido != null)
                {
                    await _usoCodigoReferidoRepository.InsertarAsync(usuarioReferido.Id, creado.Id, dto.CodigoReferenciado);
                }
            }

            var planFree = await _planRepo.GetByTipoAsync("FREE");

            if (planFree == null)
                return ApiResponse<object>.Error("500", "No existe un plan gratuito configurado");

            await _suscripcionRepository.CrearAsync(new Suscripcion
            {
                UsuarioId = creado.Id,
                PlanId = planFree.Id,

                StripeCustomerId = "",
                StripeCheckoutSessionId = "",
                StripeSubscriptionId = "",
                StripePriceId = "",
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.MaxValue,
                IsActive = true,
                Status = "active",
                AutoRenew = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                var htmlBienvenida = TemplatesEmail.PlantillaBienvenidaComercio(creado.Nombre, creado.Nombre);
                await _emailService.EnviarCorreoAsync(
                    creado.Email,
                    "¡Bienvenido a AdLocal! Tu comercio en la red local",
                    htmlBienvenida
                );

                var htmlVerificacion = TemplatesEmail.PlantillaVerificacionCorreo(creado.Nombre, codigoVerificacion);
                await _emailService.EnviarCorreoAsync(
                    creado.Email,
                    "Confirma tu correo electrónico - AdLocal",
                    htmlVerificacion
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL_WARNING] No se pudo enviar correo de bienvenida/verificación comercio: {ex.Message}");
            }

            return ApiResponse<object>.Success(
               null,
                "Usuario creado correctamente"
            );
        }
        public async Task<ApiResponse<object>> CrearAdmin(AdminCreateDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Nombre))
                    return ApiResponse<object>.Error("400", "El nombre es obligatorio");

                if (string.IsNullOrEmpty(dto.Email))
                    return ApiResponse<object>.Error("400", "El email es obligatorio");

                if (string.IsNullOrEmpty(dto.Password))
                    return ApiResponse<object>.Error("400", "La contraseña es obligatoria");

                bool existente = await _repository.ExistePorCorreoAsync(dto.Email);

                if (existente)
                    return ApiResponse<object>.Error("400", "El correo ya está registrado");

                var admin = new Usuario
                {
                    Nombre = dto.Nombre,
                    Email = dto.Email,
                    Rol = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    FechaCreacion = DateTime.UtcNow
                };

                var creado = await _repository.CreateAsync(admin);

                return ApiResponse<object>.Success(
                    new
                    {
                        creado.Id,
                        creado.Nombre,
                        creado.Email,
                        creado.Rol,
                        creado.FechaCreacion
                    },
                    "Administrador creado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> ActualizarUsuario(UsuarioUpdateDto dto)
        {
            long id = _jwtContext.GetUserId();
            var usuario = await _repository.GetByIdAsync(id);

            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");

            bool emailCambiado = !string.IsNullOrWhiteSpace(dto.Email) && !dto.Email.Trim().Equals(usuario.Email, StringComparison.OrdinalIgnoreCase);

            usuario.Nombre = dto.Nombre;
            usuario.Email = dto.Email;

            await _repository.UpdateAsync(usuario);

            if (emailCambiado)
            {
                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                await _refreshTokenService.RevocarTodosPorUsuarioAsync(usuario.Id, "Correo electrónico modificado", ip);
            }

            return ApiResponse<object>.Success(null, "Usuario actualizado correctamente");
        }

        public async Task<ApiResponse<object>> CambiarEstadoUsuario(long id, bool activo, string? motivo = null)
        {
            var usuario = await _repository.GetByIdAsync(id);
            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");

            usuario.Activo = activo;
            await _repository.UpdateAsync(usuario);

            if (!activo)
            {
                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                await _refreshTokenService.RevocarTodosPorUsuarioAsync(id, motivo ?? "Usuario bloqueado o suspendido por administrador", ip);
            }

            return ApiResponse<object>.Success(null, activo ? "Usuario activado correctamente" : "Usuario bloqueado y sesiones invalidadas correctamente");
        }

        public async Task<ApiResponse<object>> CambiarRolUsuario(long id, string nuevoRol)
        {
            if (string.IsNullOrWhiteSpace(nuevoRol))
                return ApiResponse<object>.Error("400", "El rol es requerido");

            var usuario = await _repository.GetByIdAsync(id);
            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");

            usuario.Rol = nuevoRol.Trim();
            await _repository.UpdateAsync(usuario);

            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            await _refreshTokenService.RevocarTodosPorUsuarioAsync(id, $"Rol modificado a {nuevoRol}", ip);

            return ApiResponse<object>.Success(null, $"Rol actualizado a {nuevoRol} y sesiones anteriores invalidadas.");
        }
        public async Task<ApiResponse<UsuarioInfoDto>> ObtenerInfoUsuario()
        {
            long id = _jwtContext.GetUserId();
            var usuario = await _repository.GetByIdAsync(id);

            if (usuario == null)
                return ApiResponse<UsuarioInfoDto>.Error("404", "Usuario no encontrado");

            var info = new UsuarioInfoDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol,
                ComercioId = usuario.ComercioId,
                FechaCreacion = usuario.FechaCreacion,
                Activo = usuario.Activo,
                FotoUrl = usuario.FotoUrl,
            };

            return ApiResponse<UsuarioInfoDto>.Success(info);
        }
        public async Task<ApiResponse<object>> DeleteUsuario(int id)
        {
            try
            {
                var usuario = await _repository.GetByIdAsync(id);

                if (usuario == null)
                    return ApiResponse<object>.Error("404", "Usuario no encontrado");

                await _repository.DeleteAsync(id);

                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                await _refreshTokenService.RevocarTodosPorUsuarioAsync(id, "Usuario eliminado", ip);

                return ApiResponse<object>.Success(
                    null,
                    "Usuario eliminado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<string>> UploadPhotoAsync(UploadPhotoDto dto)
        {
            long id = _jwtContext.GetUserId();
            var usuario = await _repository.GetByIdAsync(id);
            if (usuario == null)
                return ApiResponse<string>.Error("404", "Usuario no encontrado");

            if (string.IsNullOrEmpty(dto.Base64))
                return ApiResponse<string>.Error("400", "No se recibió la imagen");

            var tiposPermitidos = new List<string> { "image/jpeg", "image/jpg", "image/png", "image/webp" };

            string base64Data = dto.Base64;
            string? tipoImagen = null;

            if (base64Data.StartsWith("data:"))
            {
                var parts = base64Data.Split(',');
                if (parts.Length != 2)
                    return ApiResponse<string>.Error("400", "Formato de imagen inválido");

                tipoImagen = parts[0].Replace("data:", "").Replace(";base64", "");
                base64Data = parts[1];
            }

            if (tipoImagen != null && !tiposPermitidos.Contains(tipoImagen.ToLower()))
                return ApiResponse<string>.Error("400", "Tipo de imagen no permitido. Solo JPEG, JPG, PNG o WEBP");

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                if (!string.IsNullOrWhiteSpace(usuario.FotoUrl))
                {
                    bool deleted = await _repository.DeleteFromS3Async(usuario.FotoUrl);

                    if (!deleted)
                    {
                        return ApiResponse<string>.Error(
                            "500",
                            "No fue posible eliminar la imagen anterior"
                        );
                    }
                }

                string contentType = tipoImagen ?? "image/png";

                string newUrl = await _repository.UploadImageAsync(
                    imageBytes,
                    id,
                    contentType
                );

                await _repository.UpdateUserPhotoUrlAsync(id, newUrl);

                return ApiResponse<string>.Success(
                    newUrl,
                    "La imagen se actualizó correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Error("500", ex.Message);
            }
        }
    }
}
