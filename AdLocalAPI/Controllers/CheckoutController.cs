using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly ISuscriptionServiceV1 _service;

    public CheckoutController(ISuscriptionServiceV1 service)
    {
        _service = service;
    }

    // Tarjeta ya guardada
    [HttpPost("suscribirse")]
    public async Task<IActionResult> Suscribirse([FromBody] CheckoutRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.StripePaymentMethodId))
            return BadRequest(ApiResponse<string>.Error("400", "Tarjeta requerida"));

        var result = await _service.SuscribirseConTarjeta(
            dto.PlanId,
            dto.StripePaymentMethodId,
            dto.autoRenew
        );

        return result.Codigo == "200" ? Ok(result) : BadRequest(result);
    }

    // Tarjeta nueva (Checkout)
    [HttpPost("checkout")]
    public async Task<IActionResult> CrearCheckout([FromBody] CheckoutRequestDto dto)
    {
        var result = await _service.CrearCheckoutSuscripcion(dto.PlanId);
        return result.Codigo == "200" ? Ok(result) : BadRequest(result);
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar()
    {
        var result = await _service.CancelarPlan();
        return result.Codigo == "200" ? Ok(result) : BadRequest(result);
    }
}
