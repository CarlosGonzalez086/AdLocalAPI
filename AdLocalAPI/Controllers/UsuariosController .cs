using AdLocalAPI.Models;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuarioService _service;
        public UsuariosController(UsuarioService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1,
            int pageSize = 10,
            string orderBy = "recent",
            string search = "")
        {
            var response = await _service.GetAllUsuarios(page, pageSize, orderBy, search);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetUsuarioById(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response =  await _service.DeleteUsuario(id);
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
