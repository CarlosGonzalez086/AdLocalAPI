using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using AdLocalAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/checkout")]
public class CheckoutController : ApiControllerBase
{
    private readonly ISuscripcionService _service;

    public CheckoutController(ISuscripcionService service)
    {
        _service = service;
    }


    [HttpPost("suscribirse")]
    public async Task<IActionResult> Suscribirse([FromBody] CheckoutRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.StripePaymentMethodId))
            return Responder(ApiResponse<string>.BadRequest("Tarjeta requerida"));

        var result = await _service.SuscribirseConTarjeta(
            dto.PlanId,
            dto.StripePaymentMethodId,
            dto.autoRenew
        );

        return Responder(result);
    }


    [HttpPost("checkout")]
    public async Task<IActionResult> CrearCheckout([FromBody] CheckoutRequestDto dto)
    {
        var result = await _service.CrearCheckoutSuscripcion(dto.PlanId);
        return Responder(result);
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar()
    {
        var result = await _service.CancelarPlan();
        return Responder(result);
    }
}
