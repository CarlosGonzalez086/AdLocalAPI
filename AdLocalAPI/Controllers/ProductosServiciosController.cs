using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductosServiciosController : ApiControllerBase
    {
        private readonly IProductosServiciosService _service;

        public ProductosServiciosController(IProductosServiciosService service)
        {
            _service = service;
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ProductosServiciosDto dto)
        {
            var response = await _service.CreateAsync(dto);
            return Responder(response);
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(long id, [FromBody] ProductosServiciosDto dto)
        {
            var response = await _service.UpdateAsync(id, dto);
            return Responder(response);
        }
        [Authorize]
        [HttpDelete("{id}/idComercio/{idComercio}")]
        public async Task<IActionResult> Eliminar(long id,long idComercio = 0)
        {
            var response = await _service.DeleteAsync(id, idComercio);
            return Responder(response);
        }
        [Authorize]
        [HttpPut("desactivar/{id}/idComercio/{idComercio}")]
        public async Task<IActionResult> Desactivar(long id,long idComercio = 0)
        {
            var response = await _service.DesactivarAsync(id, idComercio);
            return Responder(response);
        }
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _service.GetByIdAsync(id);
            return Responder(response);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllPaged(
            int page = 1,
            int pageSize = 10,
            string orderBy = "recent",
            string search = "",
            long idComercio = 0
        )
        {
            var response = await _service.GetAllPagedAsync(page, pageSize, orderBy, search, idComercio);
            return Responder(response);
        }
        [Authorize]
        [HttpGet("comercio/{idComercio}")]
        public async Task<IActionResult> GetAll(long idComercio)
        {
            var response = await _service.GetAllAsync(idComercio);
            return Responder(response);
        }
    }
}
