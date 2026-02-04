using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComerciosController : ControllerBase
    {
        private readonly ComercioService _service;

        public ComerciosController(ComercioService service)
        {
            _service = service;
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ComercioCreateDto dto)
        {
            var response = await _service.CreateComercio(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Actualizar([FromBody] ComercioUpdateDto dto)
        {
            var response = await _service.UpdateComercio(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            var response = await _service.DeleteComercio(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _service.GetComercioById(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var response = await _service.GetComercioByUser();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet("getTotalComerciosByIdUsuario")]
        public async Task<IActionResult> GeTotalComerciosByIdUsuario()
        {
            var response = await _service.GeTotalComerciosByIdUsuario();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetAll(
    [FromQuery] string tipo = "populares",
    [FromQuery] double lat = 0,
    [FromQuery] double lng = 0,
    [FromQuery] string municipio = "",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10
)
        {

            var response = await _service.GetAllComercios(
                tipo,
                lat,
                lng,
                municipio,
                page,
                pageSize
            );

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }


        [HttpGet("por-filtros")]
        public async Task<IActionResult> GetByFiltros(
            [FromQuery] int estadoId = 0,
            [FromQuery] int municipioId = 0,
            [FromQuery] string orden = "alfabetico",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8
        )
        {
            var response = await _service.GetByFiltros(
                estadoId,
                municipioId,
                orden,
                page,
                pageSize
            );

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        [Authorize]
        [HttpGet("getAllComerciosByUser")]
        public async Task<IActionResult> GetAllComerciosByUser(
            int page = 1,
            int pageSize = 10
        )
        {
            var response = await _service.GetAllComerciosByUserPaged(page, pageSize);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [Authorize]
        [HttpPost("guardarColaborador")]
        public async Task<IActionResult> guardarColaborador([FromBody] ColaborarDto dto)
        {
            var response = await _service.guardarColaborador(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet("getAllColaboradores")]
        public async Task<IActionResult> getAllColaboradores(long idComercio, int page = 1,int pageSize = 10)
        {
            var response = await _service.getAllColaboradores(idComercio,page, pageSize);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpPut("toggleAccesoColaborador")]
        public async Task<IActionResult> toggleAccesoColaborador(int idColaborador, long idComercio)
        {
            var response = await _service.toggleAccesoColaborador(idColaborador, idComercio);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpDelete("eliminarColaborador")]
        public async Task<IActionResult> eliminarColaborador(int idColaborador, long idComercio)
        {
            var response = await _service.eliminarColaborador(idColaborador, idComercio);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

    }
}
