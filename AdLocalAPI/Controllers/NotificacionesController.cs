using System;
using System.Threading.Tasks;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/Notificaciones")]
    [Authorize]
    public class NotificacionesController : ApiControllerBase
    {
        private readonly INotificacionService _service;
        public NotificacionesController(INotificacionService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Obtener([FromQuery] int limite = 20) =>
            Responder(await _service.ObtenerAsync(limite));

        [HttpPut("{uuid:guid}/leida")]
        public async Task<IActionResult> MarcarLeida(Guid uuid)
        {
            var response = await _service.MarcarLeidaAsync(uuid);
            return Responder(response);
        }

        [HttpPut("leer-todas")]
        public async Task<IActionResult> MarcarTodasLeidas() =>
            Responder(await _service.MarcarTodasLeidasAsync());
    }
}
