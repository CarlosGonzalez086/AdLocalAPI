using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Tarjetas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ApiControllerBase
    {
        private readonly ITarjetaService _service;

        public TarjetasController(ITarjetaService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearTarjetaDto dto)
        {
            var response = await _service.CrearTarjeta(dto);
            return Responder(response);
        }

        [HttpPut("{id}/default")]
        public async Task<IActionResult> SetDefault(long id)
        {
            var response = await _service.SetDefault(id);
            return Responder(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            var response = await _service.EliminarTarjeta(id);
            return Responder(response);
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var response = await _service.Listar();
            return Responder(response);
        }
    }


}
