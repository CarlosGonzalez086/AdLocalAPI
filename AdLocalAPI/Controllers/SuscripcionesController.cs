using AdLocalAPI.DTOs;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/suscripciones")]
public class SuscripcionesController : ControllerBase
{
    private readonly SuscripcionService _service;

    public SuscripcionesController(SuscripcionService service)
    {
        _service = service;
    }
    [HttpGet("mi-suscripcion")]
    public async Task<IActionResult> MiSuscripcion()
    {
        var response = await _service.ObtenerMiSuscripcion();
        return response.Codigo == "200" ? Ok(response) : BadRequest(response);
    }
    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
    {
        var response = await _service.ObtenerTodasAsync(page, pageSize);
        return Ok(response);
    }
    [HttpGet("suscripciones-stats")]
    public async Task<IActionResult> SuscripcionesStats()
    {
        var response = await _service.ObtenerStatsSuscripciones();
        return Ok(response);
    }


}
