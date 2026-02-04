using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsoCodigoReferidoController : ControllerBase
    {
        private readonly UsoCodigoReferidoService _service;

        public UsoCodigoReferidoController(UsoCodigoReferidoService service)
        {
            _service = service;
        }
        [HttpGet("mis-usos")]
        public async Task<IActionResult> MisUsos()
        {
            var response = await _service.ContarMisUsosAsync();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpGet("contar")]

        public async Task<IActionResult> ContarPorCodigo(
            [FromQuery] string codigo)
        {
            var response = await _service.ContarPorCodigoAsync(codigo);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpGet("total")]

        public async Task<IActionResult> TotalUsos()
        {
            var response = await _service.ContarTotalAsync();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
