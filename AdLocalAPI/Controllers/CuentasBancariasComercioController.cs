using System;
using System.Threading.Tasks;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Comercio")]
    public class CuentasBancariasComercioController : ApiControllerBase
    {
        private readonly ICuentaBancariaComercioService _service;

        public CuentasBancariasComercioController(ICuentaBancariaComercioService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodas([FromQuery] long comercioId = 0)
        {
            var response = await _service.ObtenerTodas(comercioId);
            return Responder(response);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CuentaBancariaComercioCreateDto dto, [FromQuery] long comercioId = 0)
        {
            var response = await _service.Crear(dto, comercioId);
            return Responder(response);
        }

        [HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(Guid uuid, [FromBody] CuentaBancariaComercioUpdateDto dto, [FromQuery] long comercioId = 0)
        {
            var response = await _service.Actualizar(uuid, dto, comercioId);
            return Responder(response);
        }

        [HttpDelete("{uuid:guid}")]
        public async Task<IActionResult> Eliminar(Guid uuid, [FromQuery] long comercioId = 0)
        {
            var response = await _service.Eliminar(uuid, comercioId);
            return Responder(response);
        }

        [HttpPut("{uuid:guid}/principal")]
        public async Task<IActionResult> EstablecerPrincipal(Guid uuid, [FromQuery] long comercioId = 0)
        {
            var response = await _service.EstablecerPrincipal(uuid, comercioId);
            return Responder(response);
        }
    }
}