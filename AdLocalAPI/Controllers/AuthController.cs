using AdLocalAPI.DTOs;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UsuarioService _service;
        public AuthController(UsuarioService service)
        {
            _service = service;
        }
        [HttpPost("registro")]
        public async Task<IActionResult> RegistroUsuario([FromBody] UsuarioRegistroDto dto)
        {
            var response = await _service.CrearUsuarioCliente(dto);
            return response.Codigo == "200" ? Ok(response): BadRequest(response);
        }
        [HttpPost("login")]
        public async Task<IActionResult> LoginUsuario([FromBody] LoginDto dto)
        {
            var response = await _service.Login(dto.Email, dto.Password);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateUsuario([FromBody] UsuarioUpdateDto dto)
        {
            var response = await _service.ActualizarUsuario(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VerUsuario()
        {
            var response = await _service.ObtenerInfoUsuario();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] ChangePasswordDto dto)
        {
            var response = await _service.CambiarPassword(dto);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
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
        [Authorize]
        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto([FromBody] UploadPhotoDto dto)
        {
            var response = await _service.UploadPhotoAsync(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] EmailDto email)
        {
            var response = await _service.ForgetPassword(email.Email);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpPost("new-password")]
        public async Task<IActionResult> NewPassword([FromBody] NewPasswordDto dto)
        {
            var response = await _service.NewPassword(dto);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }


        [HttpPost("check-token")]
        public async Task<IActionResult> CheckToken(string token)
        {
            var response = await _service.CheckToken(token);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

    }
}
