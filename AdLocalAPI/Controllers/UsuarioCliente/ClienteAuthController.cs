using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente;
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
    public class ClienteAuthController : ApiControllerBase
    {
        private readonly IClienteService _service;

        public ClienteAuthController(IClienteService service)
        {
            _service = service;
        }

        // RENOVAR TOKEN
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
                await refreshTokenService.RevocarTokenAsync(token, "Logout de cliente", ip);
            }
            refreshTokenService.EliminarCookieRefreshToken(Response);
            return Responder(ApiResponse.Success("Sesión cerrada correctamente"));
        }

        // REGISTRO
        [EnableRateLimiting("auth")]
        [HttpPost("registro")]
        public async Task<IActionResult> Registro([FromBody] ClienteRegistroDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.CrearCliente(dto, cancellationToken);
            return Responder(response);
        }

        // LOGIN
        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.LoginCliente(dto, cancellationToken);
            return Responder(response);
        }

        // VERIFICAR CORREO
        [EnableRateLimiting("auth")]
        [HttpPost("verificar-correo")]
        public async Task<IActionResult> VerificarCorreo([FromBody] VerificarCorreoDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.VerificarCorreoAsync(dto, cancellationToken);
            return Responder(response);
        }

        // REENVIAR VERIFICACIÓN DE CORREO
        [EnableRateLimiting("auth")]
        [HttpPost("reenviar-verificacion")]
        public async Task<IActionResult> ReenviarVerificacion([FromBody] ReenviarVerificacionDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.ReenviarVerificacionAsync(dto, cancellationToken);
            return Responder(response);
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet("perfil")]
        public async Task<IActionResult> ObtenerPerfil(CancellationToken cancellationToken = default)
        {
            var response = await _service.ObtenerPerfilAsync(cancellationToken);
            return Responder(response);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPut("perfil")]
        public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarPerfilClienteDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.ActualizarPerfilAsync(dto, cancellationToken);
            return Responder(response);
        }

        // SOLICITAR CÓDIGO PARA RECUPERAR CONTRASEÑA
        [EnableRateLimiting("auth")]
        [HttpPost("recuperar-password")]
        public async Task<IActionResult> RecuperarPassword([FromBody] EmailDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.EnviarCodigoRecuperacion(dto, cancellationToken);
            return Responder(response);
        }

        // VERIFICAR CÓDIGO
        [EnableRateLimiting("auth")]
        [HttpPost("verificar-codigo")]
        public async Task<IActionResult> VerificarCodigo([FromBody] VerificarCodigoDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.VerificarCodigo(dto, cancellationToken);
            return Responder(response);
        }

        // ESTABLECER NUEVA CONTRASEÑA
        [EnableRateLimiting("auth")]
        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _service.RestablecerPassword(dto, cancellationToken);
            return Responder(response);
        }
    }
}
