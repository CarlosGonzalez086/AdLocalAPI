using AdLocalAPI.DTOs.Direcciones;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class DireccionesUsuarioController
        : ControllerBase
    {
        private readonly IDireccionUsuarioService _service;

        public DireccionesUsuarioController(
            IDireccionUsuarioService service)
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
            var response =
                await _service.ObtenerTodas();

            return ProcesarRespuesta(
                response.Codigo,
                response
            );
        }

        // ============================================================
        // OBTENER UNA
        // GET api/DireccionesUsuario/{uuid}
        // ============================================================

        [HttpGet("{uuid:guid}")]
        public async Task<IActionResult> ObtenerPorUuid(
            Guid uuid)
        {
            var response =
                await _service.ObtenerPorUuid(
                    uuid
                );

            return ProcesarRespuesta(
                response.Codigo,
                response
            );
        }

        // ============================================================
        // CREAR
        // POST api/DireccionesUsuario
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody]
            DireccionUsuarioDto dto)
        {
            var response =
                await _service.Crear(
                    dto
                );

            return ProcesarRespuesta(
                response.Codigo,
                response
            );
        }

        // ============================================================
        // ACTUALIZAR
        // PUT api/DireccionesUsuario/{uuid}
        // ============================================================

        [HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(
            Guid uuid,
            [FromBody]
            DireccionUsuarioDto dto)
        {
            var response =
                await _service.Actualizar(
                    uuid,
                    dto
                );

            return ProcesarRespuesta(
                response.Codigo,
                response
            );
        }

        // ============================================================
        // ELIMINAR
        // DELETE api/DireccionesUsuario/{uuid}
        // ============================================================

        [HttpDelete("{uuid:guid}")]
        public async Task<IActionResult> Eliminar(
            Guid uuid)
        {
            var response =
                await _service.Eliminar(
                    uuid
                );

            return ProcesarRespuesta(
                response.Codigo,
                response
            );
        }

        // ============================================================
        // ESTABLECER PREDETERMINADA
        // PUT api/DireccionesUsuario/{uuid}/predeterminada
        // ============================================================

        [HttpPut("{uuid:guid}/predeterminada")]
        public async Task<IActionResult>
            EstablecerPredeterminada(
                Guid uuid)
        {
            var response =
                await _service
                    .EstablecerPredeterminada(
                        uuid
                    );

            return ProcesarRespuesta(
                response.Codigo,
                response
            );
        }

        // ============================================================
        // RESPUESTAS HTTP
        // ============================================================

        private IActionResult ProcesarRespuesta(
            string codigo,
            object response)
        {
            return codigo switch
            {
                "200" =>
                    Ok(response),

                "401" =>
                    Unauthorized(response),

                "403" =>
                    StatusCode(
                        StatusCodes.Status403Forbidden,
                        response
                    ),

                "404" =>
                    NotFound(response),

                "500" =>
                    StatusCode(
                        StatusCodes.Status500InternalServerError,
                        response
                    ),

                _ =>
                    BadRequest(response)
            };
        }
    }
}