using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/Cotizaciones")]
    [Authorize]
    public class CotizacionesController : ApiControllerBase
    {
        private readonly ICotizacionService _cotizacionService;
        private readonly JwtContext _jwt;

        public CotizacionesController(ICotizacionService cotizacionService, JwtContext jwt)
        {
            _cotizacionService = cotizacionService;
            _jwt = jwt;
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearCotizacionDto dto, System.Threading.CancellationToken cancellationToken = default)
        {
            var response = await _cotizacionService.CrearCotizacionAsync(_jwt.GetUserId(), dto, cancellationToken);
            return Responder(response);
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet("mias")]
        public async Task<IActionResult> Mias(System.Threading.CancellationToken cancellationToken = default)
        {
            var response = await _cotizacionService.ObtenerMiasAsync(_jwt.GetUserId(), cancellationToken);
            return Responder(response);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPut("{uuid:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid uuid, System.Threading.CancellationToken cancellationToken = default)
        {
            var response = await _cotizacionService.CancelarCotizacionAsync(_jwt.GetUserId(), uuid, cancellationToken);
            return Responder(response);
        }
    }
}
