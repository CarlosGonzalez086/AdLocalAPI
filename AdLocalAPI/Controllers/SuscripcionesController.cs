using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace AdLocalAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SuscripcionesController : ControllerBase
    {
        private readonly SuscripcionService _service;
        public SuscripcionesController(SuscripcionService service)
        {
            _service = service;
        }

        [HttpPost("contratar")]
        public async Task<IActionResult> Contratar([FromBody] SuscripcionCreateDto dto)
        {
            var response = await _service.ContratarPlan(dto.PlanId);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpGet("mi-suscripcion")]
        public async Task<IActionResult> MiSuscripcion()
        {
            var response = await _service.ObtenerMiSuscripcion();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpPost("cancelar")]
        public async Task<IActionResult> Cancelar()
        {
            var response = await _service.CancelarMiSuscripcion();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }       

    }
}
