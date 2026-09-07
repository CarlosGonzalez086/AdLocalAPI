using System.Threading.Tasks;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComercioVisitasController : ApiControllerBase
    {
        private readonly IComercioVisitaService _service;

        public ComercioVisitasController(IComercioVisitaService service)
        {
            _service = service;
        }

        [HttpPost("{comercioId}")]
        public async Task<IActionResult> Registrar(long comercioId)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _service.RegistrarVisita(comercioId, ip);
            return Responder(response);
        }

        [Authorize]
        [HttpGet("{comercioId}/stats")]
        public async Task<IActionResult> GetStats(long comercioId)
        {
            var response = await _service.GetStats(comercioId);
            return Responder(response);
        }
    }
}
