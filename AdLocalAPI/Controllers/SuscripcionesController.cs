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

    [HttpPost("crear")]
    public async Task<IActionResult> Crear([FromBody] CrearSuscripcionDto dto)
    {
        var res = await _service.CrearCheckoutSession(dto);
        return res.Codigo == "200" ? Ok(res) : BadRequest(res);
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar()
    {
        var res = await _service.CancelarSuscripcion();
        return res.Codigo == "200" ? Ok(res) : BadRequest(res);
    }

    [HttpGet("activa")]
    public async Task<IActionResult> ObtenerActiva()
    {
        var res = await _service.ObtenerSuscripcionActiva();
        return res.Codigo == "200" ? Ok(res) : BadRequest(res);
    }

    [HttpPost("cambiar-plan")]
    public async Task<IActionResult> CambiarPlan([FromBody] CambiarPlanDto dto)
    {
        var res = await _service.CambiarPlan(dto);
        return res.Codigo == "200" ? Ok(res) : BadRequest(res);
    }
    [HttpGet("mi-suscripcion")]
    public async Task<IActionResult> MiSuscripcion()
    {
        var response = await _service.ObtenerMiSuscripcion();
        return response.Codigo == "200" ? Ok(response) : BadRequest(response);
    }
}
