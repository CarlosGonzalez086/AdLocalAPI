using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComerciosController : ControllerBase
    {
        private readonly ComercioService _service;

        public ComerciosController(ComercioService service)
        {
            _service = service;
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ComercioCreateDto dto)
        {
            var response = await _service.CreateComercio(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Actualizar([FromBody] ComercioUpdateDto dto)
        {
            var response = await _service.UpdateComercio(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var response = await _service.DeleteComercio(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetComercioById(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var response = await _service.GetComercioByUser();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string tipo = "populares",
            [FromQuery] double? lat = null,
            [FromQuery] double? lng = null
        )
        {
            var response = await _service.GetAllComercios(tipo, lat, lng);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

    }
}
