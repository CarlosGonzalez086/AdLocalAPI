using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs.Direcciones;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class DireccionesUsuarioController : ApiControllerBase
    {
        private readonly IDireccionUsuarioService _service;

        public DireccionesUsuarioController(IDireccionUsuarioService service)
        {
            _service = service;
        }

        // ============================================================
        // OBTENER TODAS
        // GET api/DireccionesUsuario
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var response = await _service.ObtenerTodas();
            return Responder(response);
        }

        // ============================================================
        // OBTENER UNA
        // GET api/DireccionesUsuario/{uuid}
        // ============================================================

        [HttpGet("{uuid:guid}")]
        public async Task<IActionResult> ObtenerPorUuid(Guid uuid)
        {
            var response = await _service.ObtenerPorUuid(uuid);
            return Responder(response);
        }

        // ============================================================
        // CREAR
        // POST api/DireccionesUsuario
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] DireccionUsuarioDto dto)
        {
            var response = await _service.Crear(dto);
            return Responder(response);
        }

        // ============================================================
        // ACTUALIZAR
        // PUT api/DireccionesUsuario/{uuid}
        // ============================================================

        [HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(Guid uuid, [FromBody] DireccionUsuarioDto dto)
        {
            var response = await _service.Actualizar(uuid, dto);
            return Responder(response);
        }

        // ============================================================
        // ELIMINAR
        // DELETE api/DireccionesUsuario/{uuid}
        // ============================================================

        [HttpDelete("{uuid:guid}")]
        public async Task<IActionResult> Eliminar(Guid uuid)
        {
            var response = await _service.Eliminar(uuid);
            return Responder(response);
        }

        // ============================================================
        // ESTABLECER PREDETERMINADA
        // PUT api/DireccionesUsuario/{uuid}/predeterminada
        // ============================================================

        [HttpPut("{uuid:guid}/predeterminada")]
        public async Task<IActionResult> EstablecerPredeterminada(Guid uuid)
        {
            var response = await _service.EstablecerPredeterminada(uuid);
            return Responder(response);
        }
    }
}