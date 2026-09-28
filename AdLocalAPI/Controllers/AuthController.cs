using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IUsuarioService _service;
        public AuthController(IUsuarioService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("renovar-token")]
        public async Task<IActionResult> RenovarToken([FromBody] RenovarTokenDto? dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.RenovarTokenAsync(dto ?? new RenovarTokenDto(), cancellationToken);
            return Responder(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RenovarTokenDto? dto, [FromServices] IRefreshTokenService refreshTokenService)
        {
            var token = refreshTokenService.ExtraerRefreshToken(Request, dto?.RefreshToken ?? dto?.TokenActual ?? dto?.Token);
            if (!string.IsNullOrEmpty(token))
            {
                var ip = HttpContext.Connection?.RemoteIpAddress?.ToString();
                await refreshTokenService.RevocarTokenAsync(token, "Logout de usuario", ip);
            }
            refreshTokenService.EliminarCookieRefreshToken(Response);
            return Responder(ApiResponse.Success("Sesión cerrada correctamente"));
        }

        [Authorize]
        [HttpGet("sesiones")]
        public async Task<IActionResult> ObtenerSesiones([FromServices] IRefreshTokenService refreshTokenService)
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Responder(ApiResponse.Unauthorized("Usuario no autenticado"));
            }

            var currentRefreshToken = refreshTokenService.ExtraerRefreshToken(Request, null);
            var sesiones = await refreshTokenService.ObtenerSesionesActivasAsync(userId, currentRefreshToken);

            return Responder(ApiResponse<List<SesionActivaDto>>.Success(sesiones, "Sesiones activas obtenidas correctamente"));
        }

        [Authorize]
        [HttpDelete("sesiones/{id:long}")]
        public async Task<IActionResult> RevocarSesion(long id, [FromServices] IRefreshTokenService refreshTokenService)
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Responder(ApiResponse.Unauthorized("Usuario no autenticado"));
            }

            var ip = HttpContext.Connection?.RemoteIpAddress?.ToString();
            var success = await refreshTokenService.RevocarSesionPorIdAsync(id, userId, "Revocada manualmente por el usuario", ip);

            if (!success)
            {
                return Responder(ApiResponse.NotFound("Sesión no encontrada o ya revocada"));
            }

            return Responder(ApiResponse.Success("Sesión revocada correctamente"));
        }

        [Authorize]
        [HttpPost("sesiones/revocar-todas")]
        public async Task<IActionResult> RevocarTodasLasSesiones([FromServices] IRefreshTokenService refreshTokenService)
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Responder(ApiResponse.Unauthorized("Usuario no autenticado"));
            }

            var ip = HttpContext.Connection?.RemoteIpAddress?.ToString();
            await refreshTokenService.RevocarTodosPorUsuarioAsync(userId, "Cierre de todas las sesiones solicitado por el usuario", ip);
            refreshTokenService.EliminarCookieRefreshToken(Response);

            return Responder(ApiResponse.Success("Todas las sesiones activas han sido revocadas"));
        }

        [EnableRateLimiting("auth")]
        [HttpPost("registro")]
        public async Task<IActionResult> RegistroUsuario([FromBody] UsuarioRegistroDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.CrearUsuarioCliente(dto, cancellationToken);
            return Responder(response);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> LoginUsuario([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.Login(dto.Email, dto.Password, cancellationToken);
            return Responder(response);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("verificar-correo")]
        public async Task<IActionResult> VerificarCorreo([FromBody] VerificarCorreoDto dto)
        {
            var response = await _service.VerificarCorreoAsync(dto);
            return Responder(response);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("reenviar-verificacion")]
        public async Task<IActionResult> ReenviarVerificacion([FromBody] ReenviarVerificacionDto dto)
        {
            var response = await _service.ReenviarVerificacionAsync(dto);
            return Responder(response);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateUsuario([FromBody] UsuarioUpdateDto dto)
        {
            var response = await _service.ActualizarUsuario(dto);
            return Responder(response);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VerUsuario()
        {
            var response = await _service.ObtenerInfoUsuario();
            return Responder(response);
        }

        [Authorize]
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] ChangePasswordDto dto)
        {
            var response = await _service.CambiarPassword(dto);
            return Responder(response);
        }

        [Authorize]
        [HttpPost("actualizar-jwt")]
        public async Task<IActionResult> ActualizarJwt([FromBody] UpdateJwtRequest request)
        {
            var result = await _service.ActualizarJwtAsync(
                request.Email,
                request.UpdateJWT
            );

            if (!result.Success)
            {
                return Responder(ApiResponse.NotFound(result.Message));
            }

            return Responder(ApiResponse<object>.Success(new
            {
                token = result.Token,
                usuario = new
                {
                    result.Usuario!.Id,
                    result.Usuario.Nombre,
                    result.Usuario.Email,
                    result.Usuario.Rol,
                    result.Usuario.ComercioId
                }
            }, result.Message));
        }

        [Authorize]
        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto([FromBody] UploadPhotoDto dto)
        {
            var response = await _service.UploadPhotoAsync(dto);
            return Responder(response);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] EmailDto email)
        {
            var response = await _service.ForgetPassword(email.Email);
            return Responder(response);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("new-password")]
        public async Task<IActionResult> NewPassword([FromBody] NewPasswordDto dto)
        {
            var response = await _service.NewPassword(dto);
            return Responder(response);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("check-token")]
        public async Task<IActionResult> CheckToken(string token)
        {
            var response = await _service.CheckToken(token);
            return Responder(response);
        }
    }
}
