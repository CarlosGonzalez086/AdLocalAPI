using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Comercio,Colaborador")]
    public class ConfiguracionPagoComercioController
        : ControllerBase
    {
        private readonly IConfiguracionPagoComercioService _service;

        public ConfiguracionPagoComercioController(
            IConfiguracionPagoComercioService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Obtener()
        {
            var response =
                await _service.Obtener();

            return response.Codigo switch
            {
                "200" => Ok(response),
                "401" => Unauthorized(response),
                "403" => StatusCode(
                    StatusCodes.Status403Forbidden,
                    response
                ),
                "404" => NotFound(response),
                _ => BadRequest(response)
            };
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(
            [FromBody]
            ConfiguracionPagoComercioDto dto)
        {
            var response =
                await _service.Guardar(dto);

            return response.Codigo switch
            {
                "200" => Ok(response),
                "401" => Unauthorized(response),
                "403" => StatusCode(
                    StatusCodes.Status403Forbidden,
                    response
                ),
                _ => BadRequest(response)
            };
        }
    }
}