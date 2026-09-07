using System;
using System.Threading.Tasks;
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
    public class PedidosComercioController : ApiControllerBase
    {
        private readonly IPedidoComercioService _service;
        public PedidosComercioController(IPedidoComercioService service) => _service = service;

        [HttpGet("comercios")]
        public async Task<IActionResult> Comercios(CancellationToken cancellationToken = default) =>
            Responder(await _service.ObtenerComerciosAsync(cancellationToken));

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] long comercioId, CancellationToken cancellationToken = default) =>
            Responder(await _service.ObtenerDashboardAsync(comercioId, cancellationToken));

        [HttpGet]
        public async Task<IActionResult> Pedidos(
            [FromQuery] long comercioId, [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10, [FromQuery] EstadoPedido? estado = null,
            CancellationToken cancellationToken = default) =>
            Responder(await _service.ObtenerPedidosAsync(comercioId, page, pageSize, estado, cancellationToken));

        [HttpGet("{pedidoUuid:guid}")]
        public async Task<IActionResult> Detalle(Guid pedidoUuid, [FromQuery] long comercioId, CancellationToken cancellationToken = default) =>
            Responder(await _service.ObtenerDetalleAsync(comercioId, pedidoUuid, cancellationToken));

        [HttpPut("{pedidoUuid:guid}/estado")]
        public async Task<IActionResult> Estado(
            Guid pedidoUuid, [FromQuery] long comercioId, [FromBody] CambiarEstadoPedidoDto dto,
            CancellationToken cancellationToken = default) =>
            Responder(await _service.CambiarEstadoAsync(comercioId, pedidoUuid, dto, cancellationToken));

        [HttpPut("{pedidoUuid:guid}/pago")]
        public async Task<IActionResult> Pago(
            Guid pedidoUuid, [FromQuery] long comercioId, [FromBody] RevisarPagoPedidoDto dto,
            CancellationToken cancellationToken = default) =>
            Responder(await _service.RevisarPagoAsync(comercioId, pedidoUuid, dto, cancellationToken));

        [HttpGet("{pedidoUuid:guid}/comprobante")]
        public async Task<IActionResult> Comprobante(Guid pedidoUuid, [FromQuery] long comercioId, CancellationToken cancellationToken = default)
        {
            var response = await _service.ObtenerComprobanteAsync(comercioId, pedidoUuid, cancellationToken);
            return response.EsExitoso && response.Respuesta != null
                ? File(response.Respuesta.Contenido, response.Respuesta.ContentType, response.Respuesta.Nombre)
                : Responder(response);
        }
    }
}
