using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ComercioVisitasController : ControllerBase
    {
        private readonly ComercioVisitaService _service;

        public ComercioVisitasController(ComercioVisitaService service)
        {
            _service = service;
        }

        [HttpPost("{comercioId}")]
        public async Task<IActionResult> Registrar(long comercioId)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _service.RegistrarVisita(comercioId, ip);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [Authorize]
        [HttpGet("{comercioId}/stats")]
        public async Task<IActionResult> GetStats(long comercioId)
        {
            var response = await _service.GetStats(comercioId);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
