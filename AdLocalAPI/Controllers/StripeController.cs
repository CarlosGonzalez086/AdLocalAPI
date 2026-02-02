using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripe;
        private readonly JwtContext _jwtContext;
        private readonly UsuarioRepository _usuarioRepository;
        public StripeController(IStripeService stripe, JwtContext jwtContext, UsuarioRepository usuarioRepository)
        {
            _stripe = stripe;
            _jwtContext = jwtContext;
            _usuarioRepository = usuarioRepository;
        }
        [HttpPost("setup-intent")]
        public async Task<IActionResult> CrearSetupIntent()
        {
            long userId = _jwtContext.GetUserId();
            var user = await _usuarioRepository.GetByIdAsync(userId);

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerId = await _stripe.CreateCustomer(user.Email);
                user.StripeCustomerId = customerId;
                await _usuarioRepository.UpdateAsync(user);
            }

            var service = new SetupIntentService();
            var setupIntent = await service.CreateAsync(new SetupIntentCreateOptions
            {
                Customer = user.StripeCustomerId,
                PaymentMethodTypes = new List<string> { "card" }
            });
            Console.WriteLine(setupIntent);

            return Ok(new { clientSecret = setupIntent.ClientSecret });
        }


    }
}
