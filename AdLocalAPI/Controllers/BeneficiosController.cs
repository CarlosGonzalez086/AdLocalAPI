using AdLocalAPI.DTOs;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BeneficiosController : ControllerBase
    {
        private readonly BeneficiosServices _service;

        public BeneficiosController(BeneficiosServices service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> ReclamarBeneficio()
        {
            var response = await _service.ReclamarBeneficio();
            return response.Codigo == "200" ? Ok(response) : BadRequest(response);
        }
    }
}
