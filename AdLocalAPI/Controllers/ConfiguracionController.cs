using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracionController : ControllerBase
    {
        private readonly IConfiguracionService _service;

        public ConfiguracionController(IConfiguracionService service)
        {
            _service = service;
        }
        [HttpGet("listar")]
        public async Task<IActionResult> Listar()
        {
            var response = await _service.ObtenerTodosAsync();

            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpPost("stripe")]
        public async Task<IActionResult> Crear([FromBody] StripeConfiguracionDto dto)
        {
            var response = await _service.RegistrarStripeAsync(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpPost("claves")]
        public async Task<IActionResult> CrearClaves([FromBody] ClavesConfigDto dto)
        {
            var response = await _service.RegistrarCrearClavesAsync(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }

}
