using System.Threading.Tasks;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsoCodigoReferidoController : ApiControllerBase
    {
        private readonly IUsoCodigoReferidoService _service;

        public UsoCodigoReferidoController(IUsoCodigoReferidoService service)
        {
            _service = service;
        }

        [HttpGet("mis-usos")]
        public async Task<IActionResult> MisUsos()
        {
            var response = await _service.ContarMisUsosAsync();
            return Responder(response);
        }

        [HttpGet("contar")]
        public async Task<IActionResult> ContarPorCodigo([FromQuery] string codigo)
        {
            var response = await _service.ContarPorCodigoAsync(codigo);
            return Responder(response);
        }

        [HttpGet("total")]
        public async Task<IActionResult> TotalUsos()
        {
            var response = await _service.ContarTotalAsync();
            return Responder(response);
        }
    }
}
