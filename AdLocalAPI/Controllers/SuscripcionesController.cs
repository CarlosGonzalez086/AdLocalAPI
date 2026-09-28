using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/suscripciones")]
    public class SuscripcionesController : ApiControllerBase
    {
    private readonly ISuscripcionService _service;
    private readonly IStripeReconciliationService _reconciliationService;
    private readonly JwtContext _jwtContext;

    public SuscripcionesController(
        ISuscripcionService service,
        IStripeReconciliationService reconciliationService,
        JwtContext jwtContext)
    {
        _service = service;
        _reconciliationService = reconciliationService;
        _jwtContext = jwtContext;
    }

    [HttpGet("mi-suscripcion")]
    public async Task<IActionResult> MiSuscripcion()
    {
        var response = await _service.ObtenerMiSuscripcion();
        return Responder(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await _service.ObtenerTodasAsync(page, pageSize);
        return Responder(response);
    }

    [HttpGet("suscripciones-stats")]
    public async Task<IActionResult> SuscripcionesStats()
    {
        var response = await _service.ObtenerStatsSuscripciones();
        return Responder(response);
    }

    [HttpPost("reconciliar")]
    public async Task<IActionResult> ReconciliarMiSuscripcion()
    {
        long usuarioId = _jwtContext.GetUserId();
        var response = await _reconciliationService.ReconciliarUsuarioAsync(usuarioId);
        return Responder(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("reconciliar-todas")]
    public async Task<IActionResult> ReconciliarTodas()
    {
        var response = await _reconciliationService.ReconciliarTodasAsync();
        return Responder(response);
    }
}
}
