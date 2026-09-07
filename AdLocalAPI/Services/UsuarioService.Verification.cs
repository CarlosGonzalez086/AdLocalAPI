using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services
{
    public partial class UsuarioService
    {
        public async Task<ApiResponse<object>> VerificarCorreoAsync(VerificarCorreoDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Codigo))
            {
                return ApiResponse<object>.Error("400", "El correo y el código son requeridos.");
            }

            var cleanEmail = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _repository.GetByCorreoAsync(cleanEmail);

            if (usuario == null)
            {
                return ApiResponse<object>.Error("400", "El código de verificación no es válido o ha expirado.");
            }

            if (usuario.EmailVerificado)
            {
                return ApiResponse<object>.Success(null, "El correo electrónico ya ha sido verificado previamente.");
            }

            // Validar bloqueo por intentos fallidos
            if (usuario.IntentosCodigo >= 5)
            {
                usuario.Codigo = null;
                usuario.CodigoExpiracion = null;
                usuario.IntentosCodigo = 0;
                await _repository.UpdateAsync(usuario);

                return ApiResponse<object>.Error(
                    "400",
                    "Ha superado el número máximo de intentos permitidos (5). Por seguridad, el código ha sido invalidado. Solicite uno nuevo."
                );
            }

            // Validar expiración del código (24 horas)
            if (usuario.CodigoExpiracion.HasValue && DateTime.UtcNow > usuario.CodigoExpiracion.Value)
            {
                usuario.Codigo = null;
                usuario.CodigoExpiracion = null;
                usuario.IntentosCodigo = 0;
                await _repository.UpdateAsync(usuario);

                return ApiResponse<object>.Error("400", "El código de verificación ha expirado. Solicite uno nuevo.");
            }

            // Comparar código
            if (string.IsNullOrWhiteSpace(usuario.Codigo) ||
                !string.Equals(usuario.Codigo.Trim(), dto.Codigo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                usuario.IntentosCodigo++;

                if (usuario.IntentosCodigo >= 5)
                {
                    usuario.Codigo = null;
                    usuario.CodigoExpiracion = null;
                    usuario.IntentosCodigo = 0;
                    await _repository.UpdateAsync(usuario);

                    return ApiResponse<object>.Error(
                        "400",
                        "Ha superado el número máximo de intentos permitidos (5). Por seguridad, el código ha sido invalidado. Solicite uno nuevo."
                    );
                }

                await _repository.UpdateAsync(usuario);

                var restantes = Math.Max(0, 5 - usuario.IntentosCodigo);
                return ApiResponse<object>.Error(
                    "400",
                    $"El código de verificación es incorrecto. Intentos restantes: {restantes}."
                );
            }

            // Verificación exitosa
            usuario.EmailVerificado = true;
            usuario.Codigo = null;
            usuario.CodigoExpiracion = null;
            usuario.IntentosCodigo = 0;
            await _repository.UpdateAsync(usuario);

            return ApiResponse<object>.Success(null, "Correo electrónico verificado exitosamente.");
        }

        public async Task<ApiResponse<object>> ReenviarVerificacionAsync(ReenviarVerificacionDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return ApiResponse<object>.Success(null, "Si el correo está registrado, recibirás un nuevo código de verificación.");
            }

            var cleanEmail = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _repository.GetByCorreoAsync(cleanEmail);

            // Mitigación de enumeración de usuarios
            if (usuario == null || usuario.EmailVerificado || !usuario.Activo)
            {
                return ApiResponse<object>.Success(null, "Si el correo está registrado, recibirás un nuevo código de verificación.");
            }

            var codigo = ServicesGenerals.GenerarCodigoAlfanumerico(6);
            usuario.Codigo = codigo;
            usuario.CodigoExpiracion = DateTime.UtcNow.AddHours(24);
            usuario.IntentosCodigo = 0;
            await _repository.UpdateAsync(usuario);

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
                Console.WriteLine($"[EMAIL_WARNING] No se pudo enviar correo de verificación: {ex.Message}");
            }

            return ApiResponse<object>.Success(null, "Si el correo está registrado, recibirás un nuevo código de verificación.");
        }
    }
}
