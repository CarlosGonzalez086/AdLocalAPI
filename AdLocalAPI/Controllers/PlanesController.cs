using AdLocalAPI.DTOs;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
 
    [ApiController]
    [Route("api/[controller]")]
    public class PlanesController : ControllerBase
    {
        private readonly PlanService _service;
        public PlanesController(PlanService service)
        {
            _service = service;
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] PlanCreateDto dto)
        {
            var response = await _service.CrearPlan(dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] PlanCreateDto dto)
        {
            var response = await _service.ActualizarPlan(id,dto);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var response = await _service.EliminarPlan(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetPlanById(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            int page = 1,
            int pageSize = 10,
            string orderBy = "recent",
            string search = ""
        )
        {
            var response = await _service.GetAllPlanes(page, pageSize, orderBy, search);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
        [HttpGet("AllPlanesUser")]
        public async Task<IActionResult> GetAllPlanesUser()
        {
            var response = await _service.GetAllPlanesUser();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

    }
}
