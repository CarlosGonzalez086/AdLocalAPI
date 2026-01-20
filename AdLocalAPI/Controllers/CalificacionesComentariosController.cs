using AdLocalAPI.DTOs;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CalificacionesComentariosController : ControllerBase
    {
        private readonly CalificacionComentarioService _service;

        public CalificacionesComentariosController(CalificacionComentarioService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CalificacionComentarioCreateDto dto)
        {
            var response = await _service.CrearComentario(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
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
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
