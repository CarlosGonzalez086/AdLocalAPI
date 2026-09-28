using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ApiControllerBase
    {
        private readonly IUsuarioService _service;

        public UsuarioController(IUsuarioService service)
        {
            _service = service;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearUsuario([FromBody] UsuarioRegistroDto dto)
        {
            var response = await _service.CrearUsuarioCliente(dto);
            return Responder(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUsuario([FromBody] LoginDto dto)
        {
            var response = await _service.Login(dto.Email, dto.Password);
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
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] ChangePasswordDto dto)
        {
            var response = await _service.CambiarPassword(dto);
            return Responder(response);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VerUsuario()
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
                return Responder(ApiResponse<object>.NotFound(result.Message));
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