using AdLocalAPI.DTOs;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController, Route("api/AdminComisiones"), Authorize(Roles = "Admin")]
    public class AdminComisionesController : ControllerBase
    {
        private readonly IComisionService _service;
        public AdminComisionesController(IComisionService service) => _service = service;
        [HttpGet("dashboard")] public async Task<IActionResult> Dashboard() => Ok(await _service.ObtenerDashboardAsync());
        [HttpGet("resumen")] public async Task<IActionResult> Resumen([FromQuery] string periodo = "semana") => Ok(await _service.ObtenerResumenAsync(periodo));
        [HttpGet("movimientos")] public async Task<IActionResult> Movimientos([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] long? comercioId = null, [FromQuery] int? estatus = null) => Ok(await _service.ObtenerMovimientosAsync(page, pageSize, comercioId, estatus));
        [HttpPut("comercios/{comercioId:long}/liquidar")] public async Task<IActionResult> Liquidar(long comercioId, [FromBody] LiquidarComisionesDto dto)
        { var response = await _service.LiquidarAsync(comercioId, dto.Periodo); return response.Codigo == "200" ? Ok(response) : NotFound(response); }
    }
}
