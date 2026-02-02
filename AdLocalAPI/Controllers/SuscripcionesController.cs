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

}
