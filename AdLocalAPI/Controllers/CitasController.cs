using AdLocalAPI.DTOs;
using AdLocalAPI.Services;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController, Route("api/Citas")]
    public class CitasController : ControllerBase
    {
        private readonly CitaService _service;
        public CitasController(CitaService service) => _service = service;
        [Authorize(Roles = "Cliente"), HttpGet("disponibilidad/{productoUuid:guid}")]
        public async Task<IActionResult> Disponibilidad(Guid productoUuid, [FromQuery] DateOnly fecha) => Responder(await _service.HorariosAsync(productoUuid, fecha));
        [Authorize(Roles = "Cliente"), HttpPost]
        public async Task<IActionResult> Crear(CrearCitaDto dto) => Responder(await _service.CrearAsync(dto));
        [Authorize(Roles = "Cliente"), HttpGet("mias")]
        public async Task<IActionResult> Mias() => Responder(await _service.MisCitasAsync());
        [Authorize(Roles = "Cliente"), HttpPut("{uuid:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid uuid, [FromQuery] string? motivo = null) => Responder(await _service.CancelarClienteAsync(uuid, motivo));
        [Authorize(Roles = "Cliente"), HttpPut("{uuid:guid}/reprogramar")]
        public async Task<IActionResult> Reprogramar(Guid uuid, ReprogramarCitaDto dto) => Responder(await _service.ReprogramarClienteAsync(uuid, dto));
        [Authorize(Roles = "Comercio,Colaborador"), HttpGet("agenda")]
        public async Task<IActionResult> Agenda([FromQuery] long comercioId, [FromQuery] DateOnly? fecha = null) => Responder(await _service.AgendaAsync(comercioId, fecha));
        [Authorize(Roles = "Comercio,Colaborador"), HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(Guid uuid, [FromQuery] long comercioId, ActualizarCitaComercioDto dto) => Responder(await _service.ActualizarAsync(comercioId, uuid, dto));
        private IActionResult Responder<T>(ApiResponse<T> r) => r.Codigo switch { "200" => Ok(r), "403" => Forbid(), "404" => NotFound(r), "409" => Conflict(r), _ => BadRequest(r) };
    }
}
