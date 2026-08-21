using AdLocalAPI.DTOs;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdLocalAPI.DTOs.UsuarioCliente;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteAuthController : ControllerBase
    {
        private readonly IClienteService _service;

        public ClienteAuthController(IClienteService service)
        {
            _service = service;
        }

        // REGISTRO
        [HttpPost("registro")]
        public async Task<IActionResult> Registro([FromBody] ClienteRegistroDto dto)
        {
            var response = await _service.CrearCliente(dto);

            return response.Codigo == "200" ? Ok(response): BadRequest(response);
        }

        // LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var response = await _service.LoginCliente(dto);

            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet("perfil")]
        public async Task<IActionResult> ObtenerPerfil()
        {
            var response = await _service.ObtenerPerfilAsync();
            return response.Codigo == "200" ? Ok(response) : NotFound(response);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPut("perfil")]
        public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarPerfilClienteDto dto)
        {
            var response = await _service.ActualizarPerfilAsync(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        // SOLICITAR CÓDIGO PARA RECUPERAR CONTRASEÑA
        [HttpPost("recuperar-password")]
        public async Task<IActionResult> RecuperarPassword([FromBody] EmailDto dto)
        {
            var response = await _service.EnviarCodigoRecuperacion(dto);

            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        // VERIFICAR CÓDIGO
        [HttpPost("verificar-codigo")]
        public async Task<IActionResult> VerificarCodigo([FromBody] VerificarCodigoDto dto)
        {
            var response = await _service.VerificarCodigo(dto);

            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        // ESTABLECER NUEVA CONTRASEÑA
        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto)
        {
            var response = await _service.RestablecerPassword(dto);

            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
