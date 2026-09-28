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
    [Authorize(Roles = "Comercio,Colaborador")]
    public class CuentasBancariasComercioController : ApiControllerBase
    {
        private readonly ICuentaBancariaComercioService _service;

        public CuentasBancariasComercioController(ICuentaBancariaComercioService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var response = await _service.ObtenerTodas();
            return Responder(response);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CuentaBancariaComercioCreateDto dto)
        {
            var response = await _service.Crear(dto);
            return Responder(response);
        }

        [HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(Guid uuid, [FromBody] CuentaBancariaComercioUpdateDto dto)
        {
            var response = await _service.Actualizar(uuid, dto);
            return Responder(response);
        }

        [HttpDelete("{uuid:guid}")]
        public async Task<IActionResult> Eliminar(Guid uuid)
        {
            var response = await _service.Eliminar(uuid);
            return Responder(response);
        }

        [HttpPut("{uuid:guid}/principal")]
        public async Task<IActionResult> EstablecerPrincipal(Guid uuid)
        {
            var response = await _service.EstablecerPrincipal(uuid);
            return Responder(response);
        }
    }
}