using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using System.Security.Cryptography;

namespace AdLocalAPI.Services
{
    public partial class ClienteService : IClienteService
    {
        public async Task<ApiResponse<object>> EnviarCodigoRecuperacion(EmailDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                {
                    return ApiResponse<object>.Error("400", "El correo electrónico es requerido.");
                }

                var email = dto.Email.Trim().ToLowerInvariant();
                var usuario = await _repository.ObtenerPorEmailAsync(email);

                if (usuario == null || !usuario.Activo)
                {
                    return ApiResponse<object>.Success(null, "Si existe una cuenta asociada al correo, recibirás un código de recuperación.");
                }

                var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                usuario.Codigo = codigo;
                usuario.CodigoExpiracion = DateTime.UtcNow.AddMinutes(15);
                usuario.Token = usuario.CodigoExpiracion.Value.Ticks.ToString();
                usuario.IntentosCodigo = 0;

                await _repository.ActualizarAsync(usuario);

                var asunto = "Código para recuperar tu contraseña - AdLocal";
                var cuerpo = TemplatesEmail.PlantillaRecuperacionPasswordCodigo(usuario.Nombre, codigo);

                await _emailService.EnviarCorreoAsync(usuario.Email, asunto, cuerpo);

                return ApiResponse<object>.Success(null, "Si existe una cuenta asociada al correo, recibirás un código de recuperación.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al solicitar la recuperación: {ex.Message}");
            }
        }

        public async Task<ApiResponse<object>> VerificarCodigo(VerificarCodigoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dto == null)
                {
                    return ApiResponse<object>.Error("400", "La información es requerida.");
                }

                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return ApiResponse<object>.Error("400", "El correo electrónico es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Codigo))
                {
                    return ApiResponse<object>.Error("400", "El código de recuperación es requerido.");
                }

                var email = dto.Email.Trim().ToLowerInvariant();
                var usuario = await _repository.ObtenerPorEmailAsync(email);

                if (usuario == null || string.IsNullOrWhiteSpace(usuario.Codigo))
                {
                    return ApiResponse<object>.Error("400", "El código no es válido o ha expirado.");
                }

                // Validar expiración de 15 minutos
                DateTime? fechaExpiracion = usuario.CodigoExpiracion;
                if (!fechaExpiracion.HasValue && !string.IsNullOrWhiteSpace(usuario.Token) && long.TryParse(usuario.Token, out var ticksExpiracion))
                {
                    fechaExpiracion = new DateTime(ticksExpiracion, DateTimeKind.Utc);
                }

                if (!fechaExpiracion.HasValue || DateTime.UtcNow > fechaExpiracion.Value)
                {
                    usuario.Codigo = null;
                    usuario.Token = null;
                    usuario.CodigoExpiracion = null;
                    usuario.IntentosCodigo = 0;

                    await _repository.ActualizarAsync(usuario);

                    return ApiResponse<object>.Error("400", "El código ha expirado. Solicita uno nuevo.");
                }

                // Validar límite de intentos
                if (usuario.IntentosCodigo >= 5)
                {
                    usuario.Codigo = null;
                    usuario.Token = null;
                    usuario.CodigoExpiracion = null;
                    usuario.IntentosCodigo = 0;

                    await _repository.ActualizarAsync(usuario);

                    return ApiResponse<object>.Error("400", "Ha superado el número máximo de intentos permitidos (5). Por seguridad, el código ha sido invalidado. Solicite uno nuevo.");
                }

                if (usuario.Codigo != dto.Codigo.Trim())
                {
                    usuario.IntentosCodigo++;
                    if (usuario.IntentosCodigo >= 5)
                    {
                        usuario.Codigo = null;
                        usuario.Token = null;
                        usuario.CodigoExpiracion = null;
                        usuario.IntentosCodigo = 0;

                        await _repository.ActualizarAsync(usuario);

                        return ApiResponse<object>.Error("400", "Ha superado el número máximo de intentos permitidos (5). Por seguridad, el código ha sido invalidado. Solicite uno nuevo.");
                    }

                    await _repository.ActualizarAsync(usuario);
                    return ApiResponse<object>.Error("400", $"El código no es válido. Intentos restantes: {5 - usuario.IntentosCodigo}.");
                }

                return ApiResponse<object>.Success(new { valido = true }, "Código verificado correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al verificar el código: {ex.Message}");
            }
        }

        public async Task<ApiResponse<object>> RestablecerPassword(RestablecerPasswordDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dto == null)
                {
                    return ApiResponse<object>.Error("400", "La información es requerida.");
                }

                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return ApiResponse<object>.Error("400", "El correo electrónico es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Codigo))
                {
                    return ApiResponse<object>.Error("400", "El código es requerido.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return ApiResponse<object>.Error("400", "La nueva contraseña es requerida.");
                }

                if (dto.Password.Length < 8)
                {
                    return ApiResponse<object>.Error("400", "La contraseña debe contener al menos 8 caracteres.");
                }

                if (dto.Password != dto.ConfirmarPassword)
                {
                    return ApiResponse<object>.Error("400", "Las contraseñas no coinciden.");
                }

                var email = dto.Email.Trim().ToLowerInvariant();
                var usuario = await _repository.ObtenerPorEmailAsync(email);

                if (usuario == null)
                {
                    return ApiResponse<object>.Error("400", "No fue posible restablecer la contraseña.");
                }

                // Validar expiración de 15 minutos
                DateTime? fechaExpiracion = usuario.CodigoExpiracion;
                if (!fechaExpiracion.HasValue && !string.IsNullOrWhiteSpace(usuario.Token) && long.TryParse(usuario.Token, out var ticksExpiracion))
                {
                    fechaExpiracion = new DateTime(ticksExpiracion, DateTimeKind.Utc);
                }

                if (!fechaExpiracion.HasValue || DateTime.UtcNow > fechaExpiracion.Value)
                {
                    usuario.Codigo = null;
                    usuario.Token = null;
                    usuario.CodigoExpiracion = null;
                    usuario.IntentosCodigo = 0;

                    await _repository.ActualizarAsync(usuario);

                    return ApiResponse<object>.Error("400", "El código ha expirado. Solicita uno nuevo.");
                }

                // Validar límite de intentos
                if (usuario.IntentosCodigo >= 5)
                {
                    usuario.Codigo = null;
                    usuario.Token = null;
                    usuario.CodigoExpiracion = null;
                    usuario.IntentosCodigo = 0;

                    await _repository.ActualizarAsync(usuario);

                    return ApiResponse<object>.Error("400", "Ha superado el número máximo de intentos permitidos (5). Por seguridad, el código ha sido invalidado. Solicite uno nuevo.");
                }

                if (string.IsNullOrWhiteSpace(usuario.Codigo) || usuario.Codigo != dto.Codigo.Trim())
                {
                    usuario.IntentosCodigo++;
                    if (usuario.IntentosCodigo >= 5)
                    {
                        usuario.Codigo = null;
                        usuario.Token = null;
                        usuario.CodigoExpiracion = null;
                        usuario.IntentosCodigo = 0;

                        await _repository.ActualizarAsync(usuario);

                        return ApiResponse<object>.Error("400", "Ha superado el número máximo de intentos permitidos (5). Por seguridad, el código ha sido invalidado. Solicite uno nuevo.");
                    }

                    await _repository.ActualizarAsync(usuario);
                    return ApiResponse<object>.Error("400", $"El código no es válido. Intentos restantes: {5 - usuario.IntentosCodigo}.");
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                usuario.Codigo = null;
                usuario.Token = null;
                usuario.CodigoExpiracion = null;
                usuario.IntentosCodigo = 0;

                await _repository.ActualizarAsync(usuario);

                var ipReset = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                await _refreshTokenService.RevocarTodosPorUsuarioAsync(usuario.Id, "Restablecimiento de contraseña", ipReset);
                if (_httpContextAccessor.HttpContext != null)
                {
                    _refreshTokenService.EliminarCookieRefreshToken(_httpContextAccessor.HttpContext.Response);
                }

                try
                {
                    var cuerpoConfirmacion = TemplatesEmail.PlantillaConfirmacionCambioPassword(usuario.Nombre);
                    await _emailService.EnviarCorreoAsync(
                        usuario.Email,
                        "Tu contraseña ha sido actualizada - AdLocal",
                        cuerpoConfirmacion
                    );
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"[EMAIL_WARNING] No se pudo enviar confirmación de contraseña: {emailEx.Message}");
                }

                return ApiResponse<object>.Success(null, "La contraseña fue actualizada correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al restablecer la contraseña: {ex.Message}");
            }
        }

        public async Task<ApiResponse<object>> VerificarCorreoAsync(VerificarCorreoDto dto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Codigo))
            {
                return ApiResponse<object>.Error("400", "El correo y el código son requeridos.");
            }

            var cleanEmail = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _repository.ObtenerPorEmailAsync(cleanEmail);

            if (usuario == null)
            {
                return ApiResponse<object>.Error("400", "El código de verificación es inválido.");
            }

            if (usuario.EmailVerificado)
            {
                return ApiResponse<object>.Success(null, "El correo electrónico ya se encuentra verificado.");
            }

            // Expiración
            if (usuario.CodigoExpiracion.HasValue && DateTime.UtcNow > usuario.CodigoExpiracion.Value)
            {
                return ApiResponse<object>.Error("400", "El código de verificación ha expirado. Solicita un nuevo código.");
            }

            // Intentos máximos
            if (usuario.IntentosCodigo >= 5)
            {
                usuario.Codigo = null;
                usuario.CodigoExpiracion = null;
                usuario.IntentosCodigo = 0;
                await _repository.ActualizarAsync(usuario);
                return ApiResponse<object>.Error("400", "Ha superado el número máximo de intentos permitidos (5). Solicite un nuevo código de verificación.");
            }

            if (usuario.Codigo != dto.Codigo.Trim())
            {
                usuario.IntentosCodigo++;
                if (usuario.IntentosCodigo >= 5)
                {
                    usuario.Codigo = null;
                    usuario.CodigoExpiracion = null;
                    usuario.IntentosCodigo = 0;
                    await _repository.ActualizarAsync(usuario);
                    return ApiResponse<object>.Error("400", "Ha superado el número máximo de intentos permitidos (5). El código ha sido invalidado. Solicite uno nuevo.");
                }
                await _repository.ActualizarAsync(usuario);
                return ApiResponse<object>.Error("400", $"El código es inválido. Intentos restantes: {5 - usuario.IntentosCodigo}.");
            }

            usuario.EmailVerificado = true;
            usuario.Codigo = null;
            usuario.CodigoExpiracion = null;
            usuario.IntentosCodigo = 0;
            await _repository.ActualizarAsync(usuario);

            return ApiResponse<object>.Success(null, "Correo electrónico verificado correctamente.");
        }

        public async Task<ApiResponse<object>> ReenviarVerificacionAsync(ReenviarVerificacionDto dto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return ApiResponse<object>.Success(null, "Si el correo está registrado, recibirás un nuevo código de verificación.");
            }

            var cleanEmail = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _repository.ObtenerPorEmailAsync(cleanEmail);

            // Mitigación de enumeración de usuarios
            if (usuario == null || usuario.EmailVerificado || !usuario.Activo)
            {
                return ApiResponse<object>.Success(null, "Si el correo está registrado, recibirás un nuevo código de verificación.");
            }

            var codigo = ServicesGenerals.GenerarCodigoAlfanumerico(6);
            usuario.Codigo = codigo;
            usuario.CodigoExpiracion = DateTime.UtcNow.AddHours(24);
            usuario.IntentosCodigo = 0;
            await _repository.ActualizarAsync(usuario);

            try
            {
                var html = TemplatesEmail.PlantillaVerificacionCorreo(usuario.Nombre, codigo);
                await _emailService.EnviarCorreoAsync(
                    usuario.Email,
                    "Verifica tu correo electrónico - AdLocal",
                    html
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL_WARNING] No se pudo enviar correo de verificación cliente: {ex.Message}");
            }

            return ApiResponse<object>.Success(null, "Si el correo está registrado, recibirás un nuevo código de verificación.");
        }
    }
}
