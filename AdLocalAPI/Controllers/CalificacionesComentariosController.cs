using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalificacionesComentariosController : ApiControllerBase
    {
        private readonly ICalificacionComentarioService _service;

        public CalificacionesComentariosController(ICalificacionComentarioService service)
        {
            _service = service;
        }

        [Authorize]
        [EnableRateLimiting("auth")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CalificacionComentarioCreateDto dto)
        {
            var response = await _service.CrearComentario(dto);
            return Responder(response);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(
            long idComercio,
            int page = 1,
            int pageSize = 10,
            string orderBy = "desc"
        )
        {
            var response = await _service.ObtenerComentarios(idComercio, page, pageSize, orderBy);
            return Responder(response);
        }
    }
}
