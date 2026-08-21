using AdLocalAPI.DTOs.Carrito;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class CarritoController : ControllerBase
    {
        private readonly ICarritoService _service;

        public CarritoController(
            ICarritoService service)
        {
            _service = service;
        }

        // ============================================================
        // OBTENER CARRITO
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Obtener()
        {
            var response =
                await _service.ObtenerCarrito();

            return ResolverRespuesta(response);
        }

        // ============================================================
        // AGREGAR PRODUCTO
        // ============================================================

        [HttpPost("agregar")]
        public async Task<IActionResult> Agregar(
            [FromBody] AgregarProductoCarritoDto dto)
        {
            var response =
                await _service.AgregarProducto(dto);

            return ResolverRespuesta(response);
        }

        // ============================================================
        // ACTUALIZAR CANTIDAD
        // ============================================================

        [HttpPut("cantidad")]
        public async Task<IActionResult> ActualizarCantidad(
            [FromBody] ActualizarCantidadCarritoDto dto)
        {
            var response =
                await _service.ActualizarCantidad(dto);

            return ResolverRespuesta(response);
        }

        // ============================================================
        // ELIMINAR PRODUCTO
        // ============================================================

        [HttpDelete("producto/{detalleUuid:guid}")]
        public async Task<IActionResult> EliminarProducto(
            Guid detalleUuid)
        {
            var response =
                await _service.EliminarProducto(
                    detalleUuid
                );

            return ResolverRespuesta(response);
        }

        // ============================================================
        // VACIAR CARRITO
        // ============================================================

        [HttpDelete("vaciar")]
        public async Task<IActionResult> Vaciar()
        {
            var response =
                await _service.VaciarCarrito();

            return ResolverRespuesta(response);
        }

        // ============================================================
        // RESPUESTAS
        // ============================================================

        private IActionResult ResolverRespuesta(ApiResponse<object> response)
        {
            return response.Codigo switch
            {
                "200" => Ok(response),

                "400" => BadRequest(response),

                "401" => Unauthorized(response),

                "403" => StatusCode(
                    StatusCodes.Status403Forbidden,
                    response
                ),

                "404" => NotFound(response),

                "409" => Conflict(response),

                "500" => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    response
                ),

                _ => BadRequest(response)
            };
        }
    }
}