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

        public StripeController(
            IStripeService stripe,
            JwtContext jwtContext,
            UsuarioRepository usuarioRepository)
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

            // Stripe YA está configurado al iniciar la API
            // (esto solo es log, puedes quitarlo luego)
            Console.WriteLine(
                $"Stripe mode: {(StripeConfiguration.ApiKey.StartsWith("sk_live") ? "LIVE" : "TEST")}"
            );

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerId = await _stripe.CreateCustomer(user.Email);
                user.StripeCustomerId = customerId;
                await _usuarioRepository.UpdateAsync(user);
            }

            var setupIntentService = new SetupIntentService();

            var setupIntent = await setupIntentService.CreateAsync(
                new SetupIntentCreateOptions
                {
                    Customer = user.StripeCustomerId,
                    PaymentMethodTypes = new List<string> { "card" }
                }
            );

            return Ok(new { clientSecret = setupIntent.ClientSecret });
        }
    }
}
