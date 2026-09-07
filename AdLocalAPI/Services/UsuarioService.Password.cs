using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;
using Microsoft.Extensions.Hosting;

namespace AdLocalAPI.Services
{
    public partial class UsuarioService
    {
        public async Task<ApiResponse<object>> CambiarPassword(ChangePasswordDto dto)
        {
            long userId = _jwtContext.GetUserId();

            var usuario = await _repository.GetByIdAsync(userId);
            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");

            bool passwordCorrecto = BCrypt.Net.BCrypt.Verify(
                dto.PasswordActual,
                usuario.PasswordHash
            );

            if (!passwordCorrecto)
                return ApiResponse<object>.Error(
                    "400",
                    "La contraseña actual es incorrecta"
                );

            if (dto.PasswordNueva.Length < 8)
                return ApiResponse<object>.Error(
                    "400",
                    "La nueva contraseña debe tener al menos 8 caracteres"
                );

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);

            await _repository.UpdateAsync(usuario);

            var ipCambio = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            await _refreshTokenService.RevocarTodosPorUsuarioAsync(usuario.Id, "Cambio de contraseña", ipCambio);

            if (_httpContextAccessor.HttpContext != null)
            {
                _refreshTokenService.EliminarCookieRefreshToken(_httpContextAccessor.HttpContext.Response);
            }

            return ApiResponse<object>.Success(
                null,
                "Contraseña actualizada correctamente. Las sesiones activas han sido cerradas por seguridad."
            );
        }

        public async Task<ApiResponse<object>> ForgetPassword(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ApiResponse<object>.Success(
                        null,
                        "Si el correo está registrado, recibirás un correo con las instrucciones para restablecer tu contraseña."
                    );
                }

                var cleanEmail = email.Trim().ToLowerInvariant();
                var usuario = await _repository.GetByCorreoAsync(cleanEmail);

                // Mitigación de enumeración de usuarios: Retornar siempre éxito sin filtrar si existe
                if (usuario == null || !usuario.Activo)
                {
                    return ApiResponse<object>.Success(
                        null,
                        "Si el correo está registrado, recibirás un correo con las instrucciones para restablecer tu contraseña."
                    );
                }

                string codigo = ServicesGenerals.GenerarCodigoAlfanumerico(6);
                string token = Guid.NewGuid().ToString();

                usuario.Codigo = codigo;
                usuario.Token = token;
                usuario.CodigoExpiracion = DateTime.UtcNow.AddMinutes(15);
                usuario.IntentosCodigo = 0;

                await _repository.UpdateAsync(usuario);

                bool esProduccion = _env.IsProduction();
                var link = UrlHelper.GenerarLinkCambioPassword(token, esProduccion, "user");

                var html = TemplatesEmail.PlantillaRecuperacionPasswordLink(usuario.Nombre, codigo, link);
                await _emailService.EnviarCorreoAsync(
                    usuario.Email,
                    "Restablecer contraseña - AdLocal",
                    html
                );

                return ApiResponse<object>.Success(
                    null,
                    "Si el correo está registrado, recibirás un correo con las instrucciones para restablecer tu contraseña."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> NewPassword(NewPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Codigo))
            {
                return ApiResponse<object>.Error("400", "El código o token de recuperación es requerido.");
            }

            var usuario = await _repository.GetByCodeAsync(dto.Codigo.Trim());
            if (usuario == null)
            {
                return ApiResponse<object>.Error("400", "El código no es válido o ha expirado.");
            }

            // Validar expiración de 15 minutos
            if (usuario.CodigoExpiracion.HasValue && DateTime.UtcNow > usuario.CodigoExpiracion.Value)
            {
                usuario.Token = null;
                usuario.Codigo = null;
                usuario.CodigoExpiracion = null;
                usuario.IntentosCodigo = 0;
                await _repository.UpdateAsync(usuario);

                return ApiResponse<object>.Error("400", "El código ha expirado. Solicita uno nuevo.");
            }

            // Validar límite de intentos
            if (usuario.IntentosCodigo >= 5)
            {
                usuario.Token = null;
                usuario.Codigo = null;
                usuario.CodigoExpiracion = null;
                usuario.IntentosCodigo = 0;
                await _repository.UpdateAsync(usuario);

                return ApiResponse<object>.Error("400", "Ha superado el número máximo de intentos permitidos (5). Por seguridad, el código ha sido invalidado. Solicite uno nuevo.");
            }

            if (string.IsNullOrWhiteSpace(dto.PasswordNueva) || dto.PasswordNueva.Length < 8)
            {
                return ApiResponse<object>.Error(
                    "400",
                    "La nueva contraseña debe tener al menos 8 caracteres"
                );
            }

            usuario.Token = null;
            usuario.Codigo = null;
            usuario.CodigoExpiracion = null;
            usuario.IntentosCodigo = 0;
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);

            await _repository.UpdateAsync(usuario);

            var ipReset = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            await _refreshTokenService.RevocarTodosPorUsuarioAsync(usuario.Id, "Restablecimiento de contraseña", ipReset);

            if (_httpContextAccessor.HttpContext != null)
            {
                _refreshTokenService.EliminarCookieRefreshToken(_httpContextAccessor.HttpContext.Response);
            }

            try
            {
                var htmlConfirmacion = TemplatesEmail.PlantillaConfirmacionCambioPassword(usuario.Nombre);
                await _emailService.EnviarCorreoAsync(
                    usuario.Email,
                    "Tu contraseña ha sido actualizada - AdLocal",
                    htmlConfirmacion
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL_WARNING] No se pudo enviar confirmación de contraseña: {ex.Message}");
            }

            return ApiResponse<object>.Success(
                null,
                "Contraseña actualizada correctamente"
            );
        }

        public async Task<ApiResponse<object>> CheckToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ApiResponse<object>.Error("400", "El enlace de recuperación no es válido.");
            }

            var usuario = await _repository.GetByTokenAsync(token.Trim());

            if (usuario == null)
            {
                return ApiResponse<object>.Error(
                    "404",
                    "El enlace de recuperación no es válido o ya ha expirado. Solicita uno nuevo."
                );
            }

            // Validar expiración de 15 minutos en el enlace
            if (usuario.CodigoExpiracion.HasValue && DateTime.UtcNow > usuario.CodigoExpiracion.Value)
            {
                usuario.Token = null;
                usuario.Codigo = null;
                usuario.CodigoExpiracion = null;
                usuario.IntentosCodigo = 0;
                await _repository.UpdateAsync(usuario);

                return ApiResponse<object>.Error(
                    "400",
                    "El enlace de recuperación ha expirado. Solicita uno nuevo."
                );
            }

            return ApiResponse<object>.Success(
                null,
                "El token es válido. Puedes continuar con el cambio de contraseña."
            );
        }
    }
}
