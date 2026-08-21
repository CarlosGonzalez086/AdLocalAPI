using AdLocalAPI.DTOs;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/PedidosComercio")]
    [Authorize(Roles = "Comercio,Colaborador")]
    public class PedidosComercioController : ControllerBase
    {
        private readonly IPedidoComercioService _service;
        public PedidosComercioController(IPedidoComercioService service) => _service = service;

        [HttpGet("comercios")]
        public async Task<IActionResult> Comercios() => Ok(await _service.ObtenerComerciosAsync());

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] long comercioId) =>
            Responder(await _service.ObtenerDashboardAsync(comercioId));

        [HttpGet]
        public async Task<IActionResult> Pedidos(
            [FromQuery] long comercioId, [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10, [FromQuery] EstadoPedido? estado = null) =>
            Responder(await _service.ObtenerPedidosAsync(comercioId, page, pageSize, estado));

        [HttpGet("{pedidoUuid:guid}")]
        public async Task<IActionResult> Detalle(Guid pedidoUuid, [FromQuery] long comercioId) =>
            Responder(await _service.ObtenerDetalleAsync(comercioId, pedidoUuid));

        [HttpPut("{pedidoUuid:guid}/estado")]
        public async Task<IActionResult> Estado(
            Guid pedidoUuid, [FromQuery] long comercioId, [FromBody] CambiarEstadoPedidoDto dto) =>
            Responder(await _service.CambiarEstadoAsync(comercioId, pedidoUuid, dto));

        [HttpPut("{pedidoUuid:guid}/pago")]
        public async Task<IActionResult> Pago(
            Guid pedidoUuid, [FromQuery] long comercioId, [FromBody] RevisarPagoPedidoDto dto) =>
            Responder(await _service.RevisarPagoAsync(comercioId, pedidoUuid, dto));

        [HttpGet("{pedidoUuid:guid}/comprobante")]
        public async Task<IActionResult> Comprobante(Guid pedidoUuid, [FromQuery] long comercioId)
        {
            var response = await _service.ObtenerComprobanteAsync(comercioId, pedidoUuid);
            return response.Codigo == "200"
                ? File(response.Respuesta.Contenido, response.Respuesta.ContentType, response.Respuesta.Nombre)
                : Responder(response);
        }

        private IActionResult Responder<T>(AdLocalAPI.Models.ApiResponse<T> response) => response.Codigo switch
        {
            "200" => Ok(response),
            "403" => Forbid(),
            "404" => NotFound(response),
            "409" => Conflict(response),
            _ => BadRequest(response)
        };
    }
}
