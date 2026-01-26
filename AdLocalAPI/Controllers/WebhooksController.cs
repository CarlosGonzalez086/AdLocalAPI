using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly SuscripcionRepository _suscripcionRepo;
    private readonly PlanRepository _planRepo;
    private readonly string _webhookSecret;

    public WebhooksController(
        IConfiguration config,
        SuscripcionRepository suscripcionRepo,
        PlanRepository planRepo)
    {
        _suscripcionRepo = suscripcionRepo;
        _planRepo = planRepo;
        _webhookSecret = config["Stripe:WebhookSecret"];
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _webhookSecret
            );
        }
        catch
        {
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.ChargeSucceeded:
                var charge = stripeEvent.Data.Object as Charge; 
                if (charge != null) 
                { 
                    await ProcesarCharge(charge); 
                }
                break;
            case EventTypes.CheckoutSessionCompleted:
                var session1 = stripeEvent.Data.Object as Session;
                if (session1 != null)
                {
                    await ProcesarCheckoutSession(session1);
                }
                break;

            case EventTypes.CustomerSubscriptionDeleted:
                await ProcesarCancelacion((Subscription)stripeEvent.Data.Object);
                break;
        }

        return Ok();
    }
    private async Task ProcesarCheckoutSession(Session session)
    {

        if (session.Metadata == null) return;

        if (!session.Metadata.TryGetValue("usuarioId", out var usuarioIdStr)) return;
        if (!session.Metadata.TryGetValue("planId", out var planIdStr)) return;

        if (!int.TryParse(usuarioIdStr, out int usuarioId)) return;
        if (!int.TryParse(planIdStr, out int planId)) return;


        if (await _suscripcionRepo.ExistePorSessionAsync(session.Id))
            return;


        var plan = await _planRepo.GetByIdAsync(planId);
        if (plan == null) return;


        var activa = await _suscripcionRepo.GetActivaByUsuarioAsync(usuarioId);
        if (activa != null)
        {
            activa.Activa = false;
            activa.Estado = "canceled";
            activa.AutoRenovacion = false;
            activa.FechaFin = DateTime.UtcNow;
            await _suscripcionRepo.ActualizarAsync(activa);
        }


        await _suscripcionRepo.CrearAsync(new Suscripcion
        {
            UsuarioId = usuarioId,
            PlanId = planId,

            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.Id,

            Monto = (decimal)(session.AmountTotal / 100m),
            Moneda = session.Currency.ToUpper(),
            MetodoPago = "stripe",

            FechaInicio = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow.AddDays(plan.DuracionDias),

            Activa = true,
            Estado = "active",
            AutoRenovacion = false
        });
    }
    private async Task ProcesarCancelacion(Subscription subscription)
    {
        var sub = await _suscripcionRepo.ObtenerPorStripeId(subscription.Id);
        if (sub == null) return;

        sub.Activa = false;
        sub.Estado = "canceled";
        sub.FechaFin = DateTime.UtcNow;

        await _suscripcionRepo.ActualizarAsync(sub);
    }
    private async Task ProcesarCharge(Charge charge)
    {
        if (!charge.Metadata.TryGetValue("usuarioId", out var usuarioIdStr)) return;
        if (!charge.Metadata.TryGetValue("planId", out var planIdStr)) return;
        if (!charge.Metadata.TryGetValue("autoRenew", out var autoRenew)) return;
        if (!int.TryParse(usuarioIdStr, out int usuarioId)) return;
        if (!int.TryParse(planIdStr, out int planId)) return;
        if (await _suscripcionRepo.ExistePorSessionAsync(charge.Id)) return;
        var plan = await _planRepo.GetByIdAsync(planId);
        if (plan == null) return;
        var activa = await _suscripcionRepo.GetActivaByUsuarioAsync(usuarioId);
        if (activa != null)
        {
            activa.Activa = false;
            activa.Estado = "canceled";
            activa.AutoRenovacion = false;
            activa.FechaFin = DateTime.UtcNow;
            await _suscripcionRepo.ActualizarAsync(activa);
        }
        await _suscripcionRepo.CrearAsync(new Suscripcion
        {
            UsuarioId = usuarioId,
            PlanId = planId,
            StripeCustomerId = charge.CustomerId,
            StripeSubscriptionId = charge.Id,
            Monto = charge.Amount / 100m,
            Moneda = charge.Currency.ToUpper(),
            MetodoPago = "stripe",
            FechaInicio = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow.AddDays(plan.DuracionDias),
            Activa = true,
            Estado = "active",
            AutoRenovacion = autoRenew == "si" ? true : false
        });
    }
}
