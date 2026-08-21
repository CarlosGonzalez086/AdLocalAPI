using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdLocalAPI.DTOs.UsuarioCliente;
using AdLocalAPI.Helpers;

namespace AdLocalAPI.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly JwtContext _jwtContext;
        private readonly UsuarioService _usuarioService;

        public ClienteService(IClienteRepository repository, IConfiguration configuration, EmailService emailService, JwtContext jwtContext, UsuarioService usuarioService)
        {
            _repository = repository;
            _configuration = configuration;
            _emailService = emailService;
            _jwtContext = jwtContext;
            _usuarioService = usuarioService;
        }

        public async Task<ApiResponse<PerfilClienteDto>> ObtenerPerfilAsync()
        {
            var usuario = await _repository.ObtenerPorIdAsync(_jwtContext.GetUserId());
            if (usuario == null || !usuario.Activo || !usuario.Rol.Equals(RolesUsuario.Cliente, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<PerfilClienteDto>.Error("404", "No se encontró el perfil.");

            return ApiResponse<PerfilClienteDto>.Success(MapearPerfil(usuario), "Perfil obtenido correctamente.");
        }

        public async Task<ApiResponse<PerfilClienteActualizadoDto>> ActualizarPerfilAsync(ActualizarPerfilClienteDto dto)
        {
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

            var token = await GenerateJwtToken(usuario);
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

        public async Task<ApiResponse<object>> CrearCliente(ClienteRegistroDto dto)
        {
            try
            {
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
                    Codigo = null
                };

                await _repository.CrearAsync(usuario);

                var token = GenerateJwtToken(usuario);

                return ApiResponse<object>.Success(token,"Cliente registrado correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al registrar al cliente: {ex.Message}");
            }
        }
        public async Task<ApiResponse<object>> LoginCliente(LoginDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ApiResponse<object>.Error("400", "Los datos de acceso son requeridos.");
                }

                var email = dto.Email?.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(email))
                {
                    return ApiResponse<object>.Error("400","El correo electrónico es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return ApiResponse<object>.Error("400","La contraseña es requerida.");
                }

                var usuario = await _repository.ObtenerPorEmailAsync(email);

                if (usuario == null)
                {
                    return ApiResponse<object>.Error("400","Correo electrónico o contraseña incorrectos.");
                }

                if (!string.Equals(usuario.Rol,"Cliente",StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<object>.Error("403","La cuenta no corresponde a un cliente.");
                }

                if (!usuario.Activo)
                {
                    return ApiResponse<object>.Error("403","La cuenta se encuentra desactivada.");
                }

                var passwordValido = BCrypt.Net.BCrypt.Verify(dto.Password,usuario.PasswordHash);

                if (!passwordValido)
                {
                    return ApiResponse<object>.Error("400","Correo electrónico o contraseña incorrectos.");
                }

                var token = GenerateJwtToken(usuario);

                return ApiResponse<object>.Success(token, "Inicio de sesión correcto.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error(
                    "500",
                    $"Ocurrió un error al iniciar sesión: {ex.Message}"
                );
            }
        }
        public async Task<ApiResponse<object>> EnviarCodigoRecuperacion(EmailDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                {
                    return ApiResponse<object>.Error("400","El correo electrónico es requerido.");
                }

                var email = dto.Email
                    .Trim()
                    .ToLowerInvariant();

                var usuario = await _repository.ObtenerPorEmailAsync(email);

                if (usuario == null || !string.Equals(usuario.Rol,"Cliente",StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<object>.Success("Si existe una cuenta asociada al correo, recibirás un código de recuperación.",null);
                }

                if (!usuario.Activo)
                {
                    return ApiResponse<object>.Success("Si existe una cuenta asociada al correo, recibirás un código de recuperación.",null);
                }

                var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                usuario.Codigo = codigo;

                /*
                 * Si agregaste una propiedad específica:
                 *
                 * usuario.CodigoRecuperacion = codigo;
                 * usuario.CodigoRecuperacionExpiracion =
                 *     DateTime.UtcNow.AddMinutes(10);
                 */

                usuario.Token = DateTime.UtcNow.AddMinutes(10).Ticks.ToString();

                await _repository.ActualizarAsync(usuario);

                var asunto = "Código para recuperar tu contraseña";

                var cuerpo = $@"
                    <div style='font-family:Arial,sans-serif'>
                        <h2>Recuperación de contraseña</h2>

                        <p>Hola {usuario.Nombre},</p>

                        <p>
                            Recibimos una solicitud para cambiar
                            la contraseña de tu cuenta de AdLocal.
                        </p>

                        <p>Tu código de recuperación es:</p>

                        <h1 style='letter-spacing:5px'>
                            {codigo}
                        </h1>

                        <p>
                            Este código tiene una vigencia de
                            10 minutos.
                        </p>

                        <p>
                            Si tú no solicitaste este cambio,
                            puedes ignorar este correo.
                        </p>
                    </div>";

                await _emailService.EnviarCorreoAsync(usuario.Email,asunto,cuerpo);

                return ApiResponse<object>.Success("Si existe una cuenta asociada al correo, recibirás un código de recuperación.",null);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500",$"Ocurrió un error al solicitar la recuperación: {ex.Message}");
            }
        }
        public async Task<ApiResponse<object>> VerificarCodigo(VerificarCodigoDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ApiResponse<object>.Error("400","La información es requerida.");
                }

                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return ApiResponse<object>.Error("400","El correo electrónico es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Codigo))
                {
                    return ApiResponse<object>.Error("400","El código de recuperación es requerido.");
                }

                var email = dto.Email.Trim().ToLowerInvariant();

                var usuario = await _repository.ObtenerPorEmailAsync(email);

                if (usuario == null || !string.Equals(usuario.Rol,"Cliente",StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<object>.Error("400","El código no es válido o ha expirado.");
                }

                if (string.IsNullOrWhiteSpace(usuario.Codigo))
                {
                    return ApiResponse<object>.Error("400","El código no es válido o ha expirado.");
                }

                if (usuario.Codigo != dto.Codigo.Trim())
                {
                    return ApiResponse<object>.Error("400","El código no es válido o ha expirado.");
                }

                /*
                 * Temporalmente estamos usando Token
                 * para guardar los ticks de expiración.
                 *
                 * Lo ideal es crear:
                 *
                 * public DateTime? CodigoRecuperacionExpiracion { get; set; }
                 */

                if (string.IsNullOrWhiteSpace(usuario.Token) || !long.TryParse(usuario.Token, out var ticksExpiracion))
                {
                    return ApiResponse<object>.Error("400","El código no es válido o ha expirado.");
                }

                var expiracion = new DateTime(ticksExpiracion,DateTimeKind.Utc);

                if (DateTime.UtcNow > expiracion)
                {
                    usuario.Codigo = null;
                    usuario.Token = null;

                    await _repository.ActualizarAsync(usuario);

                    return ApiResponse<object>.Error("400","El código ha expirado. Solicita uno nuevo.");
                }

                return ApiResponse<object>.Success(new
                {
                    valido = true
                }, "Código verificado correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500",$"Ocurrió un error al verificar el código: {ex.Message}");
            }
        }
        public async Task<ApiResponse<object>> RestablecerPassword(RestablecerPasswordDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ApiResponse<object>.Error("400","La información es requerida.");
                }

                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return ApiResponse<object>.Error("400","El correo electrónico es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Codigo))
                {
                    return ApiResponse<object>.Error("400","El código es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return ApiResponse<object>.Error("400","La nueva contraseña es requerida.");
                }

                if (dto.Password.Length < 8)
                {
                    return ApiResponse<object>.Error("400","La contraseña debe contener al menos 8 caracteres.");
                }

                if (dto.Password != dto.ConfirmarPassword)
                {
                    return ApiResponse<object>.Error("400","Las contraseñas no coinciden.");
                }

                var email = dto.Email.Trim().ToLowerInvariant();

                var usuario = await _repository.ObtenerPorEmailAsync(email);

                if (usuario == null || !string.Equals(usuario.Rol,"Cliente",StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<object>.Error("400","No fue posible restablecer la contraseña.");
                }

                if (string.IsNullOrWhiteSpace(usuario.Codigo) || usuario.Codigo != dto.Codigo.Trim())
                {
                    return ApiResponse<object>.Error("400","El código no es válido o ha expirado.");
                }

                if (string.IsNullOrWhiteSpace(usuario.Token) || !long.TryParse(usuario.Token, out var ticksExpiracion))
                {
                    return ApiResponse<object>.Error("400","El código no es válido o ha expirado.");
                }

                var expiracion = new DateTime(ticksExpiracion,DateTimeKind.Utc);

                if (DateTime.UtcNow > expiracion)
                {
                    usuario.Codigo = null;
                    usuario.Token = null;

                    await _repository.ActualizarAsync(usuario);

                    return ApiResponse<object>.Error("400","El código ha expirado. Solicita uno nuevo.");
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                /*
                 * Invalidamos inmediatamente el código.
                 */
                usuario.Codigo = null;
                usuario.Token = null;

                await _repository.ActualizarAsync(usuario);

                return ApiResponse<object>.Success("La contraseña fue actualizada correctamente.",null);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500",$"Ocurrió un error al restablecer la contraseña: {ex.Message}");
            }
        }
        public async Task<string> GenerateJwtToken(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("No se encontró la configuración Jwt:Key.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
                                        {
                                            new Claim(JwtRegisteredClaimNames.Sub,usuario.Email),
                                            new Claim(JwtRegisteredClaimNames.Email,usuario.Email),
                                            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                                            new Claim("id",usuario.Id.ToString()),
                                            new Claim("nombre",usuario.Nombre),
                                            new Claim("rol",usuario.Rol),
                                            new Claim(ClaimTypes.Role,usuario.Rol)
                                        };

            if (usuario.Rol.Equals("Cliente",StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("fotoUrl",usuario.FotoUrl ?? ""));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: null,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
