using System.Threading.Tasks;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BeneficiosController : ApiControllerBase
    {
        private readonly IBeneficiosService _service;

        public BeneficiosController(IBeneficiosService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> ReclamarBeneficio()
        {
            var response = await _service.ReclamarBeneficio();
            return Responder(response);
        }
    }
}
