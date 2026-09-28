using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdLocalAPI.Services
{
    public partial class ClienteService : IClienteService
    {
        private readonly IClienteRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly JwtContext _jwtContext;
        private readonly IUsuarioService _usuarioService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClienteService(
            IClienteRepository repository,
            IConfiguration configuration,
            IEmailService emailService,
            JwtContext jwtContext,
            IUsuarioService usuarioService,
            IRefreshTokenService refreshTokenService,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _configuration = configuration;
            _emailService = emailService;
            _jwtContext = jwtContext;
            _usuarioService = usuarioService;
            _refreshTokenService = refreshTokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<PerfilClienteDto>> ObtenerPerfilAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var usuario = await _repository.ObtenerPorIdAsync(_jwtContext.GetUserId());
            if (usuario == null || !usuario.Activo || !usuario.Rol.Equals(RolesUsuario.Cliente, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<PerfilClienteDto>.Error("404", "No se encontró el perfil.");

            return ApiResponse<PerfilClienteDto>.Success(MapearPerfil(usuario), "Perfil obtenido correctamente.");
        }

        public async Task<ApiResponse<PerfilClienteActualizadoDto>> ActualizarPerfilAsync(ActualizarPerfilClienteDto dto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var usuario = await _repository.ObtenerPorIdAsync(_jwtContext.GetUserId());
            if (usuario == null || !usuario.Activo || !usuario.Rol.Equals(RolesUsuario.Cliente, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<PerfilClienteActualizadoDto>.Error("404", "No se encontró el perfil.");

            var nombre = dto.Nombre?.Trim();
            var telefono = dto.Telefono?.Trim();
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150)
                return ApiResponse<PerfilClienteActualizadoDto>.Error("400", "Captura un nombre válido.");
            if (!string.IsNullOrWhiteSpace(telefono) &&
                (telefono.Length < 10 || telefono.Length > 20 || telefono.Any(x => !char.IsDigit(x) && x != '+' && x != ' ' && x != '-')))
                return ApiResponse<PerfilClienteActualizadoDto>.Error("400", "Captura un teléfono válido.");

            if (!string.IsNullOrWhiteSpace(dto.FotoBase64))
            {
                if (dto.FotoBase64.Length > 7_000_000)
                    return ApiResponse<PerfilClienteActualizadoDto>.Error("400", "La imagen no puede superar 5 MB.");

                var foto = await _usuarioService.UploadPhotoAsync(new UploadPhotoDto { Base64 = dto.FotoBase64 });
                if (foto.Codigo != "200")
                    return ApiResponse<PerfilClienteActualizadoDto>.Error(foto.Codigo, foto.Mensaje);
                usuario = await _repository.ObtenerPorIdAsync(usuario.Id) ?? usuario;
            }

            usuario.Nombre = nombre;
            usuario.Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono;
            usuario.FechaActualizacion = DateTime.UtcNow;
            await _repository.ActualizarAsync(usuario);

            var token = GenerateJwtToken(usuario);
            var perfil = MapearPerfil(usuario);
            return ApiResponse<PerfilClienteActualizadoDto>.Success(new PerfilClienteActualizadoDto
            {
                Nombre = perfil.Nombre,
                Email = perfil.Email,
                Telefono = perfil.Telefono,
                FotoUrl = perfil.FotoUrl,
                Token = token
            }, "Perfil actualizado correctamente.");
        }

        private static PerfilClienteDto MapearPerfil(Usuario usuario) => new()
        {
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            FotoUrl = usuario.FotoUrl
        };

        public async Task<ApiResponse<object>> CrearCliente(ClienteRegistroDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dto == null)
                {
                    return ApiResponse<object>.Error("400", "La información del cliente es requerida.");
                }

                var nombre = dto.Nombre?.Trim();
                var email = dto.Email?.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    return ApiResponse<object>.Error("400", "El nombre es requerido.");
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    return ApiResponse<object>.Error("400", "El correo electrónico es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return ApiResponse<object>.Error("400", "La contraseña es requerida.");
                }

                if (dto.Password.Length < 8)
                {
                    return ApiResponse<object>.Error("400", "La contraseña debe contener al menos 8 caracteres.");
                }

                if (dto.Password != dto.ConfirmarPassword)
                {
                    return ApiResponse<object>.Error("400", "Las contraseñas no coinciden.");
                }

                var existeEmail = await _repository.ExisteEmailAsync(email);
                if (existeEmail)
                {
                    return ApiResponse<object>.Error("400", "El correo electrónico ya está registrado.");
                }

                var codigoVerificacion = ServicesGenerals.GenerarCodigoAlfanumerico(6);

                var usuario = new Usuario
                {
                    Nombre = nombre,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Rol = "Cliente",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    ComercioId = null,
                    Token = null,
                    EmailVerificado = false,
                    Codigo = codigoVerificacion,
                    CodigoExpiracion = DateTime.UtcNow.AddHours(24),
                    IntentosCodigo = 0
                };

                await _repository.CrearAsync(usuario);

                try
                {
                    var cuerpoBienvenida = TemplatesEmail.PlantillaBienvenidaCliente(usuario.Nombre);
                    await _emailService.EnviarCorreoAsync(
                        usuario.Email,
                        "¡Bienvenido a AdLocal! Tu comunidad de comercios locales",
                        cuerpoBienvenida
                    );

                    var cuerpoVerificacion = TemplatesEmail.PlantillaVerificacionCorreo(usuario.Nombre, codigoVerificacion);
                    await _emailService.EnviarCorreoAsync(
                        usuario.Email,
                        "Confirma tu correo electrónico - AdLocal",
                        cuerpoVerificacion
                    );
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"[EMAIL_WARNING] No se pudo enviar correo de bienvenida/verificación: {emailEx.Message}");
                }

                var token = GenerateJwtToken(usuario);
                return ApiResponse<object>.Success(token, "Cliente registrado correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al registrar al cliente: {ex.Message}");
            }
        }

        public async Task<ApiResponse<object>> LoginCliente(LoginDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dto == null)
                {
                    return ApiResponse<object>.Error("400", "Los datos de acceso son requeridos.");
                }

                var email = dto.Email?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ApiResponse<object>.Error("400", "El correo electrónico es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return ApiResponse<object>.Error("400", "La contraseña es requerida.");
                }

                var usuario = await _repository.ObtenerPorEmailAsync(email);
                if (usuario == null)
                {
                    return ApiResponse<object>.Error("400", "Correo electrónico o contraseña incorrectos.");
                }

                if (!string.Equals(usuario.Rol, "Cliente", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<object>.Error("403", "La cuenta no corresponde a un cliente.");
                }

                if (!usuario.Activo)
                {
                    return ApiResponse<object>.Error("403", "La cuenta se encuentra desactivada.");
                }

                var passwordValido = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
                if (!passwordValido)
                {
                    return ApiResponse<object>.Error("400", "Correo electrónico o contraseña incorrectos.");
                }

                var token = GenerateJwtToken(usuario);

                // Generar Refresh Token y Cookie HttpOnly
                var httpContext = _httpContextAccessor.HttpContext;
                var ip = httpContext?.Connection?.RemoteIpAddress?.ToString();
                var (rawRefreshToken, _) = await _refreshTokenService.GenerarRefreshTokenAsync(usuario.Id, ip);

                if (httpContext != null)
                {
                    _refreshTokenService.EstablecerCookieRefreshToken(httpContext.Response, rawRefreshToken);
                }

                return ApiResponse<object>.Success(
                    new
                    {
                        token,
                        refreshToken = rawRefreshToken,
                        usuario = new
                        {
                            usuario.Id,
                            usuario.Nombre,
                            usuario.Email,
                            usuario.Rol,
                            usuario.EmailVerificado,
                            usuario.FotoUrl
                        }
                    },
                    "Inicio de sesión correcto."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al iniciar sesión: {ex.Message}");
            }
        }

        public string GenerateJwtToken(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("No se encontró la configuración Jwt:Key.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, usuario.Uuid.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("id", usuario.Id.ToString()),
                new Claim("nombre", usuario.Nombre),
                new Claim("rol", usuario.Rol),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("emailVerificado", usuario.EmailVerificado ? "true" : "false")
            };

            if (usuario.Rol.Equals("Cliente", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("fotoUrl", usuario.FotoUrl ?? ""));
            }

            var jwtIssuer = _configuration["Jwt:Issuer"]
                ?? _configuration["JWT:Issuer"]
                ?? Environment.GetEnvironmentVariable("JWT__Issuer")
                ?? "AdLocalAPI";

            var jwtAudience = _configuration["Jwt:Audience"]
                ?? _configuration["JWT:Audience"]
                ?? Environment.GetEnvironmentVariable("JWT__Audience")
                ?? "AdLocal";

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ApiResponse<object>> RenovarTokenAsync(RenovarTokenDto dto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _usuarioService.RenovarTokenAsync(dto, cancellationToken);
        }
    }
}
