using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ISuscriptionService _service;

    public CheckoutController(ISuscriptionService service)
    {
        _service = service;
    }

    [HttpPost("crearSesion")]
    public async Task<IActionResult> CrearSesion([FromBody] CheckoutRequestDto dto)
    {
        if (dto.Metodo == "guardada")
        {
            if (dto.StripePaymentMethodId == "") return BadRequest(ApiResponse<string>.Error("400", "Se requiere TarjetaId"));

            var result = await _service.ContratarConTarjetaGuardada(dto.PlanId, dto.StripePaymentMethodId,dto.autoRenew);
            return result.Codigo == "200" ? Ok(result) : BadRequest(result);
        }
        else if (dto.Metodo == "nueva")
        {
            var result = await _service.CrearSesionStripe(dto.PlanId);
            return result.Codigo == "200" ? Ok(result) : BadRequest(result);
        }
        else if (dto.Metodo == "transferencia")
        {
            var result = await _service.GenerarReferenciaTransferencia(dto.PlanId,dto.banco);
            return result.Codigo == "200" ? Ok(result) : BadRequest(result);
        }

        return BadRequest(ApiResponse<string>.Error("400", "Método de pago inválido"));
    }
    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar()
    {
        var result = await _service.CancelarPlan();
        return result.Codigo == "200" ? Ok(result) : BadRequest(result);
    }
}
