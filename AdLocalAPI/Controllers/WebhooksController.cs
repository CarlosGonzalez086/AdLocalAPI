using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly SuscripcionRepository _suscripcionRepo;
    private readonly PlanRepository _planRepo;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly string _webhookSecret;

    public WebhooksController(
        IConfiguration config,
        SuscripcionRepository suscripcionRepo,
        UsuarioRepository usuarioRepository,
        PlanRepository planRepo)
    {
        _suscripcionRepo = suscripcionRepo;
        _usuarioRepository = usuarioRepository;
        _planRepo = planRepo;
        _webhookSecret = config["Stripe:WebhookSecret"];
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
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
            case "checkout.session.completed":
                await OnCheckoutCompleted(
                    (Session)stripeEvent.Data.Object
                );
                break;

            case "invoice.payment_succeeded":
                await OnPaymentSucceeded(
                    (Invoice)stripeEvent.Data.Object
                );
                break;

            case "invoice.payment_failed":
                await OnPaymentFailed(
                    (Invoice)stripeEvent.Data.Object
                );
                break;

            case "customer.subscription.updated":
                await OnSubscriptionUpdated(
                    (Subscription)stripeEvent.Data.Object
                );
                break;

            case "customer.subscription.deleted":
                await OnSubscriptionDeleted(
                    (Subscription)stripeEvent.Data.Object
                );
                break;
        }

        return Ok();
    }
    private async Task OnCheckoutCompleted(Session session)
    {
        if (session.Mode != "subscription" || session.Metadata == null)
            return;

        var usuarioId = long.Parse(session.Metadata["usuarioId"]);
        var planId = int.Parse(session.Metadata["planId"]);
        var days = int.Parse(session.Metadata["days"]); // 30

        if (await _suscripcionRepo.ExistePorSessionAsync(session.Id))
            return;

        var stripeSub = await new SubscriptionService()
            .GetAsync(session.SubscriptionId);

        var existSub = await _suscripcionRepo.GetActivaByUsuarioAsync(usuarioId);
        if (existSub != null)
        {
            existSub.Status = "canceled";
            existSub.IsActive = false;
            existSub.CanceledAt = DateTime.UtcNow;
            existSub.UpdatedAt = DateTime.UtcNow;
            await _suscripcionRepo.ActualizarAsync(existSub);
        }


        DateTime periodStart = DateTime.UtcNow;
        DateTime periodEnd;

        if (stripeSub.CancelAt == DateTime.Now.AddMonths(1))
        {
            periodEnd = stripeSub.CancelAt.Value;
        }
        else
        {
            periodEnd = periodStart.AddDays(days);
        }

        await _suscripcionRepo.CrearAsync(new Suscripcion
        {
            UsuarioId = usuarioId,
            PlanId = planId,

            StripeCustomerId = stripeSub.CustomerId,
            StripeSubscriptionId = stripeSub.Id,
            StripePriceId = stripeSub.Items.Data[0].Price.Id,
            StripeCheckoutSessionId = session.Id,

            Status = "active",
            CurrentPeriodStart = periodStart,
            CurrentPeriodEnd = periodEnd,

            AutoRenew = stripeSub.CancelAt == null,
            IsActive = true
        });
    }
    private async Task OnPaymentSucceeded(Invoice invoice)
    {
        var line = invoice.Lines.Data.FirstOrDefault();
        if (line == null)
            return;

        var user = await _usuarioRepository.GetByStripeId(invoice.CustomerId);
        if (user == null)
            return;

        var json = JObject.Parse(line.RawJObject.ToString());

        string subscriptionId = json["parent"]?["subscription_item_details"]?["subscription"]?.ToString();
        if (string.IsNullOrEmpty(subscriptionId))
            return;

        var stripeSub = await new SubscriptionService()
            .GetAsync(subscriptionId);

        var sub = await _suscripcionRepo.ObtenerPorStripeId(subscriptionId);

        var existSub = await _suscripcionRepo.GetActivaByUsuarioAsync(user.Id);
        if (existSub != null && (sub == null || existSub.Id != sub.Id))
        {
            existSub.Status = "canceled";
            existSub.IsActive = false;
            existSub.CanceledAt = DateTime.UtcNow;
            existSub.UpdatedAt = DateTime.UtcNow;
            await _suscripcionRepo.ActualizarAsync(existSub);
        }

        DateTime periodStart = DateTime.UtcNow;
        DateTime periodEnd;

        if (stripeSub.CancelAt != null)
        {
            periodEnd = stripeSub.CancelAt.Value;
        }
        else
        {
            periodEnd = periodStart.AddMonths(1);
        }

        if (sub == null)
        {
            string priceId = json["pricing"]?["price_details"]?["price"]?.ToString();
            var plan = await _planRepo.GetByStripePriceIdAsync(priceId);
            if (plan == null)
                return;

            sub = new Suscripcion
            {
                UsuarioId = user.Id,
                PlanId = plan.Id,

                StripeCustomerId = invoice.CustomerId,
                StripeSubscriptionId = subscriptionId,
                StripePriceId = priceId,

                Status = "active",
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,

                AutoRenew = stripeSub.CancelAt == null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _suscripcionRepo.CrearAsync(sub);
            return;
        }

        sub.CurrentPeriodStart = periodStart;
        sub.CurrentPeriodEnd = periodEnd;
        sub.Status = "active";
        sub.IsActive = true;
        sub.UpdatedAt = DateTime.UtcNow;

        await _suscripcionRepo.ActualizarAsync(sub);
    }
    private async Task OnPaymentFailed(Invoice invoice)
    {
        var line = invoice.Lines.Data.FirstOrDefault();
        if (line == null || string.IsNullOrEmpty(line.SubscriptionId))
            return;

        var sub = await _suscripcionRepo
            .ObtenerPorStripeId(line.SubscriptionId);

        if (sub == null)
            return;

        sub.Status = "past_due";
        sub.IsActive = false;
        sub.UpdatedAt = DateTime.UtcNow;

        await _suscripcionRepo.ActualizarAsync(sub);
    }
    private async Task OnSubscriptionUpdated(Subscription stripeSub)
    {
        var sub = await _suscripcionRepo
            .ObtenerPorStripeId(stripeSub.Id);

        if (sub == null)
            return;

        if (stripeSub.Metadata["beneficio"] == "referidos_15")
        {
            sub.CurrentPeriodEnd = sub.CurrentPeriodEnd.Value.AddDays(30);

            sub.UpdatedAt = DateTime.UtcNow;

            await _suscripcionRepo.ActualizarAsync(sub);
            return;
        }

        if (stripeSub.CancelAtPeriodEnd)
        {
            sub.Status = "canceling";
            sub.IsActive = true;
            sub.AutoRenew = false;
            sub.CanceledAt = sub.CurrentPeriodEnd;
            sub.UpdatedAt = DateTime.UtcNow;
            await _suscripcionRepo.ActualizarAsync(sub);
            return;
        }
        else
        {
            sub.Status = stripeSub.Status;
            sub.IsActive = stripeSub.Status == "active";
            sub.AutoRenew = true;
            sub.CanceledAt = null;
        }

        if (stripeSub.Items?.Data?.Any() == true)
        {
            sub.StripePriceId = stripeSub.Items.Data[0].Price.Id;
        }

        await _suscripcionRepo.ActualizarAsync(sub);
    }
    private async Task OnSubscriptionDeleted(Subscription stripeSub)
    {
        var sub = await _suscripcionRepo
            .ObtenerPorStripeId(stripeSub.Id);

        if (sub == null)
            return;

        sub.Status = "canceled";
        sub.IsActive = false;
        sub.CanceledAt = DateTime.UtcNow;
        sub.UpdatedAt = DateTime.UtcNow;
        await _suscripcionRepo.ActualizarAsync(sub);

        var planFree = await _planRepo.GetByTipoAsync("FREE");
        if (planFree == null)
            return;

        await _suscripcionRepo.CrearAsync(new Suscripcion
        {
            UsuarioId = sub.UsuarioId,
            PlanId = planFree.Id,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.MaxValue,
            Status = "active",
            IsActive = true,
            AutoRenew = false,
            CreatedAt = DateTime.UtcNow
        });
    }
}
