using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Tarjetas;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ControllerBase
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
            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpPut("{id}/default")]
        public async Task<IActionResult> SetDefault(long id)
        {
            var response = await _service.SetDefault(id);
            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            var response = await _service.EliminarTarjeta(id);
            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var response = await _service.Listar();

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }
    }


}
