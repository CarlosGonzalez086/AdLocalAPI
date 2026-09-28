using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace AdLocalAPI.Services
{
    public partial class UsuarioService
    {
        public async Task<string> GenerateJwtToken(Usuario usuario)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "No se encontró la configuración Jwt:Key."
                );
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("id", usuario.Id.ToString()),
                new Claim("nombre", usuario.Nombre),
                new Claim("rol", usuario.Rol),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("emailVerificado", usuario.EmailVerificado ? "true" : "false")
            };

            if (usuario.Rol.Equals("Comercio", StringComparison.OrdinalIgnoreCase))
            {
                var comercio = await _comercioRepository.GetComercioByUser(usuario.Id);

                claims.Add(new Claim("comercioId", comercio?.Id.ToString() ?? "0"));
                claims.Add(new Claim("RedeemRewards", usuario.RedeemRewards ? "true" : "false"));
                claims.Add(new Claim("RedeemMonthFree", usuario.RedeemMonthFree ? "true" : "false"));
                claims.Add(new Claim("codigoReferido", usuario.CodigoReferido ?? ""));
                claims.Add(new Claim("fotoUrl", usuario.FotoUrl ?? ""));

                var suscripcion = await _suscripcionRepository.GetActivaByUsuarioAsync(usuario.Id);

                if (suscripcion != null && suscripcion.Plan != null)
                {
                    var plan = suscripcion.Plan;

                    claims.Add(new Claim("planId", plan.Id.ToString()));
                    claims.Add(new Claim("planTipo", plan.Tipo ?? "FREE"));
                    claims.Add(new Claim("nivelVisibilidad", plan.NivelVisibilidad.ToString()));
                    claims.Add(new Claim("estado", suscripcion.Status ?? ""));
                    claims.Add(new Claim("maxNegocios", plan.MaxNegocios.ToString()));
                    claims.Add(new Claim("maxProductos", plan.MaxProductos.ToString()));
                    claims.Add(new Claim("maxFotos", plan.MaxFotos.ToString()));
                    claims.Add(new Claim("permiteCatalogo", plan.PermiteCatalogo.ToString()));
                    claims.Add(new Claim("tieneAnalytics", plan.TieneAnalytics.ToString()));
                    claims.Add(new Claim("badge", plan.TieneBadge ? plan.BadgeTexto ?? "" : ""));
                }
                else
                {
                    claims.Add(new Claim("planTipo", "FREE"));
                    claims.Add(new Claim("nivelVisibilidad", "0"));
                }
            }
            else if (usuario.Rol.Equals("Colaborador", StringComparison.OrdinalIgnoreCase))
            {
                if (usuario.ComercioId.HasValue)
                {
                    var comercio = await _comercioRepository.GetByIdAsync(usuario.ComercioId.Value);
                    claims.Add(new Claim("comercioId", comercio?.Id.ToString() ?? "0"));

                    var usuarioManager = await _repository.GetByIdComercioAsync(usuario.ComercioId.Value);

                    if (usuarioManager != null)
                    {
                        var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(usuarioManager.Id);

                        if (planActivo != null && planActivo.Plan != null)
                        {
                            var plan = planActivo.Plan;

                            claims.Add(new Claim("planId", plan.Id.ToString()));
                            claims.Add(new Claim("planTipo", plan.Tipo ?? "FREE"));
                            claims.Add(new Claim("nivelVisibilidad", plan.NivelVisibilidad.ToString()));
                            claims.Add(new Claim("estado", planActivo.Status ?? ""));
                            claims.Add(new Claim("maxProductos", plan.MaxProductos.ToString()));
                            claims.Add(new Claim("maxFotos", plan.MaxFotos.ToString()));
                            claims.Add(new Claim("permiteCatalogo", plan.PermiteCatalogo.ToString()));
                            claims.Add(new Claim("tieneAnalytics", plan.TieneAnalytics.ToString()));
                            claims.Add(new Claim("badge", plan.TieneBadge ? plan.BadgeTexto ?? "" : ""));
                        }
                    }
                }

                claims.Add(new Claim("fotoUrl", usuario.FotoUrl ?? ""));
            }
            else if (usuario.Rol.Equals("Cliente", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("fotoUrl", usuario.FotoUrl ?? ""));
            }

            var jwtIssuer = _config["Jwt:Issuer"]
                ?? _config["JWT:Issuer"]
                ?? Environment.GetEnvironmentVariable("JWT__Issuer")
                ?? "AdLocalAPI";

            var jwtAudience = _config["Jwt:Audience"]
                ?? _config["JWT:Audience"]
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

        private ClaimsPrincipal? ObtenerPrincipalDeTokenExpirado(string token)
        {
            var jwtKey = _config["Jwt:Key"]
                ?? _config["JWT:Key"]
                ?? Environment.GetEnvironmentVariable("JWT__Key");

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                return null;
            }

            var jwtIssuer = _config["Jwt:Issuer"]
                ?? _config["JWT:Issuer"]
                ?? Environment.GetEnvironmentVariable("JWT__Issuer")
                ?? "AdLocalAPI";

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
                if (long.TryParse(expClaim, out var expSeconds))
                {
                    var fechaExpiracion = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                    if (DateTime.UtcNow > fechaExpiracion.AddDays(7))
                    {
                        return null;
                    }
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ApiResponse<object>> RenovarTokenAsync(RenovarTokenDto dto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var httpContext = _httpContextAccessor.HttpContext;
            var ip = httpContext?.Connection?.RemoteIpAddress?.ToString();

            var rawRefreshToken = httpContext != null
                ? _refreshTokenService.ExtraerRefreshToken(httpContext.Request, dto?.RefreshToken ?? dto?.TokenActual ?? dto?.Token)
                : (dto?.RefreshToken ?? dto?.TokenActual ?? dto?.Token);

            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return ApiResponse<object>.Error("400", "El token de refresco es requerido para renovar la sesión.");
            }

            var (success, error, newRawToken, usuario) = await _refreshTokenService.RotarRefreshTokenAsync(rawRefreshToken, ip);

            if (!success || usuario == null)
            {
                if (httpContext != null)
                {
                    _refreshTokenService.EliminarCookieRefreshToken(httpContext.Response);
                }

                return ApiResponse<object>.Error("401", error ?? "Sesión expirada o inválida. Inicia sesión nuevamente.");
            }

            var newAccessToken = await GenerateJwtToken(usuario);

            if (httpContext != null && !string.IsNullOrEmpty(newRawToken))
            {
                _refreshTokenService.EstablecerCookieRefreshToken(httpContext.Response, newRawToken);
            }

            return ApiResponse<object>.Success(
                new
                {
                    Token = newAccessToken,
                    RefreshToken = newRawToken,
                    Usuario = new
                    {
                        usuario.Id,
                        usuario.Nombre,
                        usuario.Email,
                        usuario.Rol,
                        usuario.EmailVerificado,
                        usuario.ComercioId
                    }
                },
                "Token renovado correctamente"
            );
        }
    }
}
