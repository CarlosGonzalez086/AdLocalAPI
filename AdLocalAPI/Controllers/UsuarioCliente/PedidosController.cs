using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdLocalAPI.Utils;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;

namespace AdLocalAPI.Controllers.UsuarioCliente
{
    [ApiController]
    [Route("api/Pedidos")]
    [Authorize(Roles = "Cliente")]
    public class PedidosController : ControllerBase
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
            [FromQuery] EstadoPagoPedido? estadoPago = null)
        {
            return Ok(await _pedidoService.ObtenerTodosAsync(
                page, pageSize, estadoPago));
        }

        [HttpGet("{pedidoUuid:guid}")]
        public async Task<IActionResult> ObtenerDetalle(Guid pedidoUuid)
        {
            var response = await _pedidoService.ObtenerDetalleAsync(pedidoUuid);

            return response.Codigo == "200"
                ? Ok(response)
                : NotFound(response);
        }

        [HttpPost("{pedidoUuid:guid}/comprobante-transferencia")]
        [Consumes("application/json")]
        public async Task<IActionResult> SubirComprobante(
            Guid pedidoUuid,
            [FromBody] SubirComprobanteTransferenciaDto comprobante)
        {
            var response = await _service.SubirAsync(pedidoUuid, comprobante);

            return response.Codigo switch
            {
                "200" => Ok(response),
                "404" => NotFound(response),
                "409" => Conflict(response),
                _ => BadRequest(response)
            };
        }
    }
}
