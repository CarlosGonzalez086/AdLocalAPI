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
    public class AdminController : ApiControllerBase
    {
        private readonly IUsuarioService _service;

        public AdminController(IUsuarioService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("renovar-token")]
        public async Task<IActionResult> RenovarToken([FromBody] RenovarTokenDto? dto)
        {
            var response = await _service.RenovarTokenAsync(dto ?? new RenovarTokenDto());
            return Responder(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RenovarTokenDto? dto, [FromServices] IRefreshTokenService refreshTokenService)
        {
            var token = refreshTokenService.ExtraerRefreshToken(Request, dto?.RefreshToken ?? dto?.TokenActual ?? dto?.Token);
            if (!string.IsNullOrEmpty(token))
            {
                var ip = HttpContext.Connection?.RemoteIpAddress?.ToString();
                await refreshTokenService.RevocarTokenAsync(token, "Logout de admin", ip);
            }
            refreshTokenService.EliminarCookieRefreshToken(Response);
            return Responder(ApiResponse.Success("Sesión cerrada correctamente"));
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearAdmin([FromBody] AdminCreateDto dto)
        {
            var response = await _service.CrearAdmin(dto);
            return Responder(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto dto)
        {
            var response = await _service.Login(dto.Email, dto.Password);
            return Responder(response);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateAdmin([FromBody] UsuarioUpdateDto dto)
        {
            var response = await _service.ActualizarUsuario(dto);
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
        [HttpGet]
        public async Task<IActionResult> VerAdmin()
        {
            var response = await _service.ObtenerInfoUsuario();
            return Responder(response);
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] EmailDto email)
        {
            var response = await _service.ForgetPassword(email.Email);
            return Responder(response);
        }

        [HttpPost("new-password")]
        public async Task<IActionResult> NewPassword([FromBody] NewPasswordDto dto)
        {
            var response = await _service.NewPassword(dto);
            return Responder(response);
        }

        [HttpPost("check-token")]
        public async Task<IActionResult> CheckToken(string token)
        {
            var response = await _service.CheckToken(token);
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
    }
}
