using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ConfiguracionController : ControllerBase
    {
        private readonly IConfiguracionService _service;

        public ConfiguracionController(
            IConfiguracionService service)
        {
            _service = service;
        }

        // ==========================================
        // LISTAR CONFIGURACIONES
        // ==========================================


        [HttpGet("listar")]
        public async Task<IActionResult> Listar()
        {
            var response =
                await _service.ObtenerTodosAsync();

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        // ==========================================
        // STRIPE
        // ==========================================

        [HttpPost("stripe")]
        public async Task<IActionResult> CrearStripe(
            [FromBody] StripeConfiguracionDto dto)
        {
            var response =
                await _service.RegistrarStripeAsync(dto);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        // ==========================================
        // CLAVES
        // ==========================================


        [HttpPost("claves")]
        public async Task<IActionResult> CrearClaves(
            [FromBody] ClavesConfigDto dto)
        {
            var response =
                await _service.RegistrarCrearClavesAsync(dto);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }

        // ==========================================
        // COMISIÓN MARKETPLACE
        // ==========================================


        [HttpPost("comision-marketplace")]
        public async Task<IActionResult> GuardarComisionMarketplace(
            [FromBody] ComisionMarketplaceDto dto)
        {
            var response =
                await _service.RegistrarComisionMarketplaceAsync(dto);

            return response.Codigo == "200"
                ? Ok(response)
                : BadRequest(response);
        }
    }
}