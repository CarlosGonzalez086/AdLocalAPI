using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CitasController : ControllerBase
    {
        private readonly ICitaService _service;

        public CitasController(
            ICitaService service)
        {
            _service = service;
        }

        // ==========================================
        // DISPONIBILIDAD
        // ==========================================

        [Authorize(Roles = "Cliente")]
        [HttpGet("disponibilidad/{productoUuid:guid}")]
        public async Task<IActionResult> Disponibilidad(
            Guid productoUuid,
            [FromQuery] DateOnly fecha)
        {
            var response =
                await _service.HorariosAsync(
                    productoUuid,
                    fecha);

            return Responder(response);
        }

        // ==========================================
        // CREAR CITA
        // ==========================================

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] CrearCitaDto dto)
        {
            var response =
                await _service.CrearAsync(dto);

            return Responder(response);
        }

        // ==========================================
        // MIS CITAS
        // ==========================================

        [Authorize(Roles = "Cliente")]
        [HttpGet("mias")]
        public async Task<IActionResult> Mias()
        {
            var response =
                await _service.MisCitasAsync();

            return Responder(response);
        }

        // ==========================================
        // CANCELAR CITA CLIENTE
        // ==========================================

        [Authorize(Roles = "Cliente")]
        [HttpPut("{uuid:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(
            Guid uuid,
            [FromQuery] string? motivo = null)
        {
            var response =
                await _service.CancelarClienteAsync(
                    uuid,
                    motivo);

            return Responder(response);
        }

        // ==========================================
        // REPROGRAMAR CITA CLIENTE
        // ==========================================

        [Authorize(Roles = "Cliente")]
        [HttpPut("{uuid:guid}/reprogramar")]
        public async Task<IActionResult> Reprogramar(
            Guid uuid,
            [FromBody] ReprogramarCitaDto dto)
        {
            var response =
                await _service.ReprogramarClienteAsync(
                    uuid,
                    dto);

            return Responder(response);
        }

        // ==========================================
        // AGENDA DEL COMERCIO
        // ==========================================

        [Authorize(Roles = "Comercio,Colaborador")]
        [HttpGet("agenda")]
        public async Task<IActionResult> Agenda(
            [FromQuery] long comercioId,
            [FromQuery] DateOnly? fecha = null)
        {
            var response =
                await _service.AgendaAsync(
                    comercioId,
                    fecha);

            return Responder(response);
        }

        // ==========================================
        // ACTUALIZAR CITA DESDE COMERCIO
        // ==========================================

        [Authorize(Roles = "Comercio,Colaborador")]
        [HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(
            Guid uuid,
            [FromQuery] long comercioId,
            [FromBody] ActualizarCitaComercioDto dto)
        {
            var response =
                await _service.ActualizarAsync(
                    comercioId,
                    uuid,
                    dto);

            return Responder(response);
        }

        // ==========================================
        // RESPUESTAS
        // ==========================================

        private IActionResult Responder<T>(
            ApiResponse<T> response)
        {
            return response.Codigo switch
            {
                "200" => Ok(response),

                "400" => BadRequest(response),

                "401" => Unauthorized(response),

                "403" => StatusCode(
                    StatusCodes.Status403Forbidden,
                    response),

                "404" => NotFound(response),

                "409" => Conflict(response),

                "500" => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    response),

                _ => BadRequest(response)
            };
        }
    }
}