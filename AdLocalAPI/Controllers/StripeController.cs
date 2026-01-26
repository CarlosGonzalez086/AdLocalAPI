using AdLocalAPI.DTOs;
using AdLocalAPI.Repositories;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Supabase.Gotrue;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StripeController : ControllerBase
    {
        private readonly UsuarioRepository _usuarioRepo;
        private readonly PlanRepository _planRepo;
        private readonly IConfiguration _config;

        public StripeController(
            UsuarioRepository usuarioRepo,
            PlanRepository planRepo,
            IConfiguration config)
        {
            _usuarioRepo = usuarioRepo;
            _planRepo = planRepo;
            _config = config;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CrearCheckout([FromBody] CreateCheckoutDto dto)
        {
            // 🔐 Usuario autenticado
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);
            var usuario = await _usuarioRepo.GetByIdAsync(userId);
            if (usuario == null)
                return BadRequest("Usuario no válido");

            // 📦 Plan
            var plan = await _planRepo.GetByIdAsync(dto.PlanId);
            if (plan == null || !plan.Activo)
                return BadRequest("Plan no válido");

            // 💰 PriceId por tipo
            if (!StripePriceIds.Planes.TryGetValue(plan.Tipo.ToUpper(), out var priceId))
                return BadRequest("PriceId no configurado");

            // 👤 Stripe Customer
            if (string.IsNullOrEmpty(usuario.StripeCustomerId))
            {
                var customerService = new Stripe.CustomerService();
                var customer = await customerService.CreateAsync(new Stripe.CustomerCreateOptions
                {
                    Email = usuario.Email
                });

                usuario.StripeCustomerId = customer.Id;
                await _usuarioRepo.UpdateAsync(usuario);
            }

            // 🧾 Checkout Session
            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                Customer = usuario.StripeCustomerId,

                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },

                SuccessUrl = $"{_config["Stripe:FrontendUrl"]}/checkout/success",
                CancelUrl = $"{_config["Stripe:FrontendUrl"]}/checkout/cancel",

                Metadata = new Dictionary<string, string>
                {
                    { "usuarioId", usuario.Id.ToString() },
                    { "planId", plan.Id.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new
            {
                url = session.Url
            });
        }
    }
}
