using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _service;

        public CheckoutController(
            ICheckoutService service)
        {
            _service = service;
        }

        // ==========================================
        // OBTENER INFORMACIÓN DEL CHECKOUT
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Obtener()
        {
            var response =
                await _service.ObtenerCheckout();

            return response.Codigo switch
            {
                "200" => Ok(response),

                "404" => NotFound(response),

                _ => BadRequest(response)
            };
        }

        // ==========================================
        // CONFIRMAR
        // ==========================================

        [HttpPost("confirmar")]
        public async Task<IActionResult> Confirmar(
            [FromBody]
            ConfirmarCheckoutDto dto)
        {
            var response =
                await _service.Confirmar(dto);

            return response.Codigo switch
            {
                "200" => Ok(response),

                "404" => NotFound(response),

                _ => BadRequest(response)
            };
        }
    }
}