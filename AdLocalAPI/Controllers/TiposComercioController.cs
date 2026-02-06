using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.TipoComercio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TiposComercioController : ControllerBase
    {
        private readonly ITipoComercioService _service;

        public TiposComercioController(ITipoComercioService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] TipoComercioCreateDto dto)
        {
            var response = await _service.Crear(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(long id, [FromBody] TipoComercioCreateDto dto)
        {
            var response = await _service.Actualizar(id, dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            var response = await _service.Eliminar(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _service.GetById(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet("getAllPaged")]
        public async Task<IActionResult> GetAllPaged(
            int page = 1,
            int pageSize = 10,
            string orderBy = "recent",
            string search = ""
        )
        {
            var response = await _service.GetAllPagedAsync(page, pageSize, orderBy, search);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpGet("getAllForSelect")]
        public async Task<IActionResult> GetForSelect()
        {
            var response = await _service.GetAllForSelectAsync();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

    }
}
