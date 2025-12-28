using AdLocalAPI.DTOs;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UsuarioService _service;

        public AdminController(UsuarioService service)
        {
            _service = service;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearAdmin([FromBody] AdminCreateDto dto)
        {
            var response = await _service.CrearAdmin(dto);
            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto dto)
        {
            var response = await _service.Login(dto.Email, dto.Password);
            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateAdmin([FromBody] UsuarioUpdateDto dto)
        {
            var response = await _service.ActualizarUsuario(dto);
            return response.Codigo == "200"
          ? Ok(response)
          : BadRequest(response);
        }
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] ChangePasswordDto dto)
        {
            var response = await _service.CambiarPassword(dto);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpGet]
        public async Task<IActionResult> VerAdmin()
        {
            var response = await _service.ObtenerInfoUsuario();
            return response.Codigo == "200"
          ? Ok(response)
          : BadRequest(response);
        }
        [HttpPost("actualizar-jwt")]
        public async Task<IActionResult> ActualizarJwt([FromBody] UpdateJwtRequest request)
        {
            var result = await _service.ActualizarJwtAsync(
                request.Email,
                request.UpdateJWT
            );

            if (!result.Success)
            {
                return NotFound(new
                {
                    codigo = "404",
                    mensaje = result.Message,
                    respuesta = ""
                });
            }

            return Ok(new
            {
                codigo = "200",
                mensaje = result.Message,
                respuesta = new
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
                }
            });
        }
    }
}
