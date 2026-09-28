using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ConfiguracionController : ApiControllerBase
    {
        private readonly IConfiguracionService _service;

        public ConfiguracionController(IConfiguracionService service)
        {
            _service = service;
        }

        // ==========================================
        // LISTAR CONFIGURACIONES
        // ==========================================

        [HttpGet("listar")]
        public async Task<IActionResult> Listar()
        {
            var response = await _service.ObtenerTodosAsync();
            return Responder(response);
        }

        // ==========================================
        // STRIPE
        // ==========================================

        [HttpPost("stripe")]
        public async Task<IActionResult> CrearStripe([FromBody] StripeConfiguracionDto dto)
        {
            var response = await _service.RegistrarStripeAsync(dto);
            return Responder(response);
        }

        // ==========================================
        // CLAVES
        // ==========================================

        [HttpPost("claves")]
        public async Task<IActionResult> CrearClaves([FromBody] ClavesConfigDto dto)
        {
            var response = await _service.RegistrarCrearClavesAsync(dto);
            return Responder(response);
        }

        // ==========================================
        // COMISIÓN MARKETPLACE
        // ==========================================

        [HttpPost("comision-marketplace")]
        public async Task<IActionResult> GuardarComisionMarketplace([FromBody] ComisionMarketplaceDto dto)
        {
            var response = await _service.RegistrarComisionMarketplaceAsync(dto);
            return Responder(response);
        }

        // ==========================================
        // CORREO
        // ==========================================

        [HttpPost("correo")]
        public async Task<IActionResult> GuardarCorreo([FromBody] EmailConfiguracionDto dto)
        {
            var response = await _service.RegistrarEmailAsync(dto);
            return Responder(response);
        }

        // ==========================================
        // PROBAR CORREO
        // ==========================================

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("probar-correo")]
        public async Task<IActionResult> ProbarCorreo([FromBody] EmailDto dto)
        {
            var response = await _service.ProbarCorreoAsync(dto?.Email);
            return Responder(response);
        }
    }
}