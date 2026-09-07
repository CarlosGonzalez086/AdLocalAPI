using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ApiControllerBase
    {
        private readonly IUsuarioService _service;
        public UsuariosController(IUsuarioService service)
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
            return Responder(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetUsuarioById(id);
            return Responder(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response =  await _service.DeleteUsuario(id);
            return Responder(response);
        }

        [HttpPatch("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(long id, [FromBody] CambiarEstadoUsuarioDto dto)
        {
            var response = await _service.CambiarEstadoUsuario(id, dto.Activo, dto.Motivo);
            return Responder(response);
        }

        [HttpPatch("{id}/rol")]
        public async Task<IActionResult> CambiarRol(long id, [FromBody] CambiarRolUsuarioDto dto)
        {
            var response = await _service.CambiarRolUsuario(id, dto.NuevoRol);
            return Responder(response);
        }

        [HttpPost("{id}/revocar-sesiones")]
        public async Task<IActionResult> RevocarSesionesUsuario(long id, [FromServices] IRefreshTokenService refreshTokenService)
        {
            var ip = HttpContext.Connection?.RemoteIpAddress?.ToString();
            await refreshTokenService.RevocarTodosPorUsuarioAsync(id, "Sesiones revocadas por el administrador", ip);
            return Responder(ApiResponse<object>.Success(null, "Todas las sesiones activas del usuario han sido revocadas."));
        }

        [HttpGet("{id}/sesiones")]
        public async Task<IActionResult> ObtenerSesionesUsuario(long id, [FromServices] IRefreshTokenService refreshTokenService)
        {
            var sesiones = await refreshTokenService.ObtenerSesionesActivasAsync(id);
            return Responder(ApiResponse<List<SesionActivaDto>>.Success(sesiones, "Sesiones activas obtenidas correctamente"));
        }
    }
}
