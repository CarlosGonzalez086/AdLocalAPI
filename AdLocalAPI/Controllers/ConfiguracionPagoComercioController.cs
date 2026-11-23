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
    public class ConfiguracionPagoComercioController : ApiControllerBase
    {
        private readonly IConfiguracionPagoComercioService _service;

        public ConfiguracionPagoComercioController(IConfiguracionPagoComercioService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Obtener([FromQuery] long comercioId = 0)
        {
            var response = await _service.Obtener(comercioId);
            return Responder(response);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromBody] ConfiguracionPagoComercioDto dto, [FromQuery] long comercioId = 0)
        {
            var response = await _service.Guardar(dto, comercioId);
            return Responder(response);
        }
    }
}