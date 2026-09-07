using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs.Carrito;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class CarritoController : ApiControllerBase
    {
        private readonly ICarritoService _service;

        public CarritoController(ICarritoService service)
        {
            _service = service;
        }

        // ============================================================
        // OBTENER CARRITO
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Obtener()
        {
            var response = await _service.ObtenerCarrito();
            return Responder(response);
        }

        // ============================================================
        // AGREGAR PRODUCTO
        // ============================================================

        [HttpPost("agregar")]
        public async Task<IActionResult> Agregar([FromBody] AgregarProductoCarritoDto dto)
        {
            var response = await _service.AgregarProducto(dto);
            return Responder(response);
        }

        // ============================================================
        // ACTUALIZAR CANTIDAD
        // ============================================================

        [HttpPut("cantidad")]
        public async Task<IActionResult> ActualizarCantidad([FromBody] ActualizarCantidadCarritoDto dto)
        {
            var response = await _service.ActualizarCantidad(dto);
            return Responder(response);
        }

        // ============================================================
        // ELIMINAR PRODUCTO
        // ============================================================

        [HttpDelete("producto/{detalleUuid:guid}")]
        public async Task<IActionResult> EliminarProducto(Guid detalleUuid)
        {
            var response = await _service.EliminarProducto(detalleUuid);
            return Responder(response);
        }

        // ============================================================
        // VACIAR CARRITO
        // ============================================================

        [HttpDelete("vaciar")]
        public async Task<IActionResult> Vaciar()
        {
            var response = await _service.VaciarCarrito();
            return Responder(response);
        }
    }
}