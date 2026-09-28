using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class CheckoutController : ApiControllerBase
    {
        private readonly ICheckoutService _service;

        public CheckoutController(ICheckoutService service)
        {
            _service = service;
        }

        // ==========================================
        // OBTENER INFORMACIÓN DEL CHECKOUT
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Obtener(CancellationToken cancellationToken = default)
        {
            var response = await _service.ObtenerCheckout(cancellationToken);
            return Responder(response);
        }

        // ==========================================
        // CONFIRMAR
        // ==========================================

        [HttpPost("confirmar")]
        public async Task<IActionResult> Confirmar(
            [FromBody] ConfirmarCheckoutDto dto,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                dto.IdempotencyKey = idempotencyKey.Trim();
            }

            var response = await _service.Confirmar(dto, cancellationToken);
            return Responder(response);
        }
    }
}