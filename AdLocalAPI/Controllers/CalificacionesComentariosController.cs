using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

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
