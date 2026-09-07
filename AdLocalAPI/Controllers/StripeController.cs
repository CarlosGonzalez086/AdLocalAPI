using System.Threading.Tasks;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StripeController : ApiControllerBase
    {
        private readonly IStripeService _stripe;
        private readonly JwtContext _jwtContext;

        public StripeController(
            IStripeService stripe,
            JwtContext jwtContext)
        {
            _stripe = stripe;
            _jwtContext = jwtContext;
        }

        [HttpPost("setup-intent")]
        public async Task<IActionResult> CrearSetupIntent()
        {
            var response = await _stripe.CrearSetupIntentParaUsuarioAsync(_jwtContext.GetUserId());
            return Responder(response);
        }
    }
}
