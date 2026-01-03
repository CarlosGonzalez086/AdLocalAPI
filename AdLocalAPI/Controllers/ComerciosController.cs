using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services;
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

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ComercioCreateDto dto)
        {
            var response = await _service.CreateComercio(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ComercioUpdateDto dto)
        {
            var response = await _service.UpdateComercio(id, dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

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
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var response = await _service.GetComercioByUser();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _service.GetAllComercios();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
