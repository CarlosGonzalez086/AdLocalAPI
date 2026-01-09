using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosServiciosController : ControllerBase
    {
        private readonly IProductosServiciosService _service;

        public ProductosServiciosController(IProductosServiciosService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ProductosServiciosDto dto)
        {
            var response = await _service.CreateAsync(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(long id, [FromBody] ProductosServiciosDto dto)
        {
            var response = await _service.UpdateAsync(id, dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            var response = await _service.DeleteAsync(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpPatch("desactivar/{id}")]
        public async Task<IActionResult> Desactivar(long id)
        {
            var response = await _service.DesactivarAsync(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _service.GetByIdAsync(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
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
        [HttpGet("{idComercio}")]
        public async Task<IActionResult> GetAll(long idComercio)
        {
            var response = await _service.GetAllAsync(idComercio);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
