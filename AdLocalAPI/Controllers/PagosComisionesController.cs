using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/PagosComisiones")]
    public class PagosComisionesController : ApiControllerBase
    {
        private readonly IPagoComisionService _pagoComisionService;
        private readonly JwtContext _jwt;

        public PagosComisionesController(IPagoComisionService pagoComisionService, JwtContext jwt)
        {
            _pagoComisionService = pagoComisionService;
            _jwt = jwt;
        }

        [Authorize(Roles = "Comercio")]
        [HttpGet("comercio/{comercioId:long}")]
        public async Task<IActionResult> Estado(long comercioId)
        {
            var response = await _pagoComisionService.ObtenerEstadoAsync(_jwt.GetUserId(), _jwt.GetUserRole(), comercioId);
            return Responder(response);
        }

        [Authorize(Roles = "Comercio")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearPagoComisionDto dto)
        {
            var response = await _pagoComisionService.CrearPagoAsync(_jwt.GetUserId(), _jwt.GetUserRole(), dto);
            return Responder(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> ListarAdmin([FromQuery] int? estatus = null)
        {
            var response = await _pagoComisionService.ListarAdminAsync(estatus);
            return Responder(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{uuid:guid}/revisar")]
        public async Task<IActionResult> Revisar(Guid uuid, [FromBody] RevisarPagoComisionDto dto)
        {
            var response = await _pagoComisionService.RevisarPagoAsync(_jwt.GetUserId(), uuid, dto);
            return Responder(response);
        }

        [Authorize]
        [HttpGet("{uuid:guid}/comprobante")]
        public async Task<IActionResult> Comprobante(Guid uuid)
        {
            var archivo = await _pagoComisionService.ObtenerComprobanteAsync(_jwt.GetUserId(), _jwt.GetUserRole(), uuid);
            if (archivo.CodigoError != null)
            {
                return Responder(ApiResponse.Error(archivo.CodigoError, archivo.MensajeError!));
            }
            return File(archivo.Contenido!, archivo.ContentType);
        }
    }
}
