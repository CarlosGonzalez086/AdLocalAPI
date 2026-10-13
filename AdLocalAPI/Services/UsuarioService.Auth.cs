using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services
{
    public partial class UsuarioService
    {
        public async Task<ApiResponse<object>> Login(string email, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var usuario = await _repository.GetByCorreoAsync(email);

            if (usuario == null)
                return ApiResponse<object>.Error("401", "Credenciales inválidas");

            if (!usuario.Activo)
                return ApiResponse<object>.Error("403", "La cuenta se encuentra inactiva o bloqueada");

            if (usuario.Rol == "Colaborador")
            {
                if (!usuario.ComercioId.HasValue)
                {
                    return ApiResponse<object>.Error("403", "El colaborador no tiene un comercio asignado.");
                }

                var usuarioManager = await _repository.GetByIdComercioAsync(usuario.ComercioId.Value);
                if (usuarioManager == null)
                {
                    return ApiResponse<object>.Error("403", "No se encontró el administrador del comercio asignado.");
                }
                var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(usuarioManager.Id);

                if (planActivo == null || planActivo.Plan == null)
                {
                    return ApiResponse<object>.Error(
                        "403",
                        "El comercio no cuenta con una suscripción activa para registrar colaboradores."
                    );
                }

                if (planActivo.CurrentPeriodEnd <= DateTime.UtcNow)
                {
                    return ApiResponse<object>.Error(
                        "403",
                        "El comercio que te dio de alta debe renovar su suscripción para agregar colaboradores."
                    );
                }
            }

            bool valid = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

            if (!valid)
                return ApiResponse<object>.Error("401", "Credenciales inválidas");

            var token = await GenerateJwtToken(usuario);

            // Generar Refresh Token y Cookie HttpOnly
            var httpContext = _httpContextAccessor.HttpContext;
            var ip = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var (rawRefreshToken, _) = await _refreshTokenService.GenerarRefreshTokenAsync(usuario.Id, ip);

            if (httpContext != null)
            {
                _refreshTokenService.EstablecerCookieRefreshToken(httpContext.Response, rawRefreshToken);
            }

            object respuesta;

            if (usuario.Rol == "Admin")
            {
                respuesta = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    usuario.EmailVerificado,
                    Token = token,
                    RefreshToken = rawRefreshToken
                };
            }
            else if (usuario.Rol == "Comercio")
            {
                respuesta = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    usuario.EmailVerificado,
                    usuario.ComercioId,
                    Token = token,
                    RefreshToken = rawRefreshToken
                };
            }
            else if (usuario.Rol == "Colaborador")
            {
                respuesta = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    usuario.EmailVerificado,
                    usuario.ComercioId,
                    Token = token,
                    RefreshToken = rawRefreshToken
                };
            }
            else
            {
                return ApiResponse<object>.Error("403", "Rol no autorizado");
            }

            return ApiResponse<object>.Success(
                respuesta,
                "Inicio de sesión exitoso"
            );
        }

        public async Task<UpdateJwtResult> ActualizarJwtAsync(string email, bool updateJWT)
        {
            if (!_jwtContext.EstaAutenticado())
                return new UpdateJwtResult { Codigo = "401", Message = "Usuario no autenticado" };

            long userId;
            try { userId = _jwtContext.GetUserId(); }
            catch (UnauthorizedAccessException)
            {
                return new UpdateJwtResult { Codigo = "401", Message = "Identidad de sesión inválida" };
            }

            // La identidad procede de la sesión validada, nunca del correo enviado.
            var usuario = await _repository.GetByIdAsync(userId);

            if (usuario == null || !usuario.Activo)
                return new UpdateJwtResult { Codigo = "401", Message = "La sesión ya no es válida" };

            if (string.IsNullOrWhiteSpace(email) ||
                !string.Equals(email.Trim(), usuario.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Security.ForeignJwtDenied ActorId={ActorId}", userId);
                return new UpdateJwtResult { Codigo = "403", Message = "Solo puedes actualizar tu propia sesión" };
            }


            string? token = null;

            if (updateJWT)
            {
                token = await GenerateJwtToken(usuario);
            }

            return new UpdateJwtResult
            {
                Success = true,
                Message = updateJWT
                    ? "JWT actualizado correctamente"
                    : "JWT no actualizado",
                Token = token,
                Usuario = usuario
            };
        }
    }
}
