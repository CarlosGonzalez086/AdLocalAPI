using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers.UsuarioCliente
{
    [ApiController]
    [Route("api/Pedidos")]
    [Authorize(Roles = "Cliente")]
    public class PedidosController : ApiControllerBase
    {
        private readonly IComprobantePagoService _service;
        private readonly IPedidoClienteService _pedidoService;

        public PedidosController(
            IComprobantePagoService service,
            IPedidoClienteService pedidoService)
        {
            _service = service;
            _pedidoService = pedidoService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] EstadoPagoPedido? estadoPago = null,
            CancellationToken cancellationToken = default)
        {
            var response = await _pedidoService.ObtenerTodosAsync(page, pageSize, estadoPago, cancellationToken);
            return Responder(response);
        }

        [HttpGet("{pedidoUuid:guid}")]
        public async Task<IActionResult> ObtenerDetalle(Guid pedidoUuid, CancellationToken cancellationToken = default)
        {
            var response = await _pedidoService.ObtenerDetalleAsync(pedidoUuid, cancellationToken);
            return Responder(response);
        }

        [HttpPost("{pedidoUuid:guid}/comprobante-transferencia")]
        [Consumes("application/json")]
        public async Task<IActionResult> SubirComprobante(
            Guid pedidoUuid,
            [FromBody] SubirComprobanteTransferenciaDto comprobante,
            CancellationToken cancellationToken = default)
        {
            var response = await _service.SubirAsync(pedidoUuid, comprobante, cancellationToken);
            return Responder(response);
        }
    }
}
