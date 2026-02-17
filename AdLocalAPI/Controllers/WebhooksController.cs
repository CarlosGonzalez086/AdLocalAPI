using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Stripe;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly SuscripcionRepository _suscripcionRepo;
    private readonly PlanRepository _planRepo;
    private readonly UsuarioRepository _usuarioRepo;
    private readonly string _webhookSecret;

    public WebhooksController(
        IConfiguration config,
        SuscripcionRepository suscripcionRepo,
        UsuarioRepository usuarioRepo,
        PlanRepository planRepo)
    {
        _suscripcionRepo = suscripcionRepo;
        _usuarioRepo = usuarioRepo;
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
                    (Stripe.Checkout.Session)stripeEvent.Data.Object
                );
                break;

            case "invoice.payment_succeeded":
                await OnInvoicePaymentSucceeded(
                    (Invoice)stripeEvent.Data.Object
                );
                break;

            case "invoice.payment_failed":
                await OnInvoicePaymentFailed(
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

    // =================================================
    // 1️ Checkout: SOLO referencia
    // =================================================
    private async Task OnCheckoutCompleted(Stripe.Checkout.Session session)
    {
        if (session.Mode != "subscription")
            return;

        if (await _suscripcionRepo.ExistePorSessionAsync(session.Id))
            return;

        await _suscripcionRepo.CrearAsync(new Suscripcion
        {
            UsuarioId = long.Parse(session.Metadata["usuarioId"]),
            PlanId = int.Parse(session.Metadata["planId"]),
            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.SubscriptionId,
            StripeCheckoutSessionId = session.Id,
            Status = "pending",
            IsActive = false,
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    // =================================================
    // 2️ Pago exitoso: AQUÍ nacen las fechas
    // =================================================
    private async Task OnInvoicePaymentSucceeded(Invoice invoice)
    {
        if (invoice?.Lines?.Data == null || !invoice.Lines.Data.Any())
            return;
        var subscriptionId = invoice.Lines.Data
            .FirstOrDefault(l =>
                l.Parent?.SubscriptionItemDetails?.Subscription != null)
            ?.Parent.SubscriptionItemDetails.Subscription;

        long usuarioId = 0;

        var user = await _usuarioRepo.GetByStripeId(invoice.CustomerId);
        if (user == null)
            return;

        var subService = new SubscriptionService();
        
        var stripeSub = await subService.GetAsync(
           subscriptionId,
            new SubscriptionGetOptions
            {
                Expand = new List<string> { "items.data.price" }
            });

        var periodStart = stripeSub.Items.Data[0].CurrentPeriodStart;
        var periodEnd = stripeSub.Items.Data[0].CurrentPeriodEnd;
        var CreatedAt = stripeSub.Items.Data[0].Created;

        var sub = await _suscripcionRepo
            .ObtenerPorStripeId(stripeSub.Id);

       

        if (sub == null)
        {
            var existSub = await _suscripcionRepo.GetActivaByUsuarioAsync(user.Id);
            if (existSub != null)
            {
                existSub.Status = "canceled";
                existSub.IsActive = false;
                existSub.CanceledAt = DateTime.UtcNow;
                existSub.UpdatedAt = DateTime.UtcNow;
                await _suscripcionRepo.ActualizarAsync(existSub);
            }
            Task.Delay(1000);
            var priceId = stripeSub.Items.Data[0].Price.Id;
            var plan = await _planRepo.GetByStripePriceIdAsync(priceId);
            if (plan == null) return;
            try 
            {
                await _suscripcionRepo.CrearAsync(new Suscripcion
                {
                    UsuarioId = user.Id,
                    PlanId = plan.Id,
                    StripeCustomerId = invoice.CustomerId,
                    StripeSubscriptionId = stripeSub.Id,
                    StripePriceId = priceId,
                    CurrentPeriodStart = periodStart,
                    CurrentPeriodEnd = periodEnd,
                    Status = "active",
                    IsActive = true,
                    AutoRenew = stripeSub.CancelAtPeriodEnd == false,
                    CreatedAt = CreatedAt
                });
            } 
            catch (Exception ex)
            { 
                Console.WriteLine(ex.ToString()); 
            }


            return;
        }

        sub.CurrentPeriodStart = periodStart;
        sub.CurrentPeriodEnd = periodEnd;
        sub.Status = "active";
        sub.IsActive = true;
        sub.UpdatedAt = DateTime.UtcNow;

        await _suscripcionRepo.ActualizarAsync(sub);
    }

    // =================================================
    // 3️ Pago fallido
    // =================================================
    private async Task OnInvoicePaymentFailed(Invoice invoice)
    {
        if (invoice?.Lines?.Data == null || !invoice.Lines.Data.Any())
            return;

        // Stripe define la suscripción en la línea
        var subscriptionId = invoice.Lines.Data
            .FirstOrDefault(l => !string.IsNullOrEmpty(l.SubscriptionId))?
            .Subscription;

        if (string.IsNullOrEmpty(subscriptionId.Id))
            return;

        var sub = await _suscripcionRepo
            .ObtenerPorStripeId(subscriptionId.Id);

        if (sub == null)
            return;

        sub.Status = "past_due";
        sub.IsActive = false;
        sub.UpdatedAt = DateTime.UtcNow;

        await _suscripcionRepo.ActualizarAsync(sub);
    }


    // =================================================
    // 4️ Cambios de estado
    // =================================================
    private async Task OnSubscriptionUpdated(Subscription stripeSub)
    {
        var sub = await _suscripcionRepo
            .ObtenerPorStripeId(stripeSub.Id);

        if (sub == null)
            return;

        if (stripeSub.CancelAtPeriodEnd)
        {
            sub.Status = "canceling";
            sub.AutoRenew = false;
            sub.CanceledAt = sub.CurrentPeriodEnd;
        }
        else
        {
            sub.Status = stripeSub.Status;
            sub.AutoRenew = true;
            sub.CanceledAt = null;
        }

        if (stripeSub.Items?.Data?.Any() == true)
            sub.StripePriceId = stripeSub.Items.Data[0].Price.Id;

        sub.UpdatedAt = DateTime.UtcNow;
        await _suscripcionRepo.ActualizarAsync(sub);
    }

    // =================================================
    // 5️ Eliminada
    // =================================================
    private async Task OnSubscriptionDeleted(Subscription stripeSub)
    {

        var sub = await _suscripcionRepo
            .ObtenerPorStripeId(stripeSub.Id);
        var planFree = await _planRepo.GetByTipoAsync("FREE");
        if (sub == null)
        {
            var user = await _usuarioRepo.GetByStripeId(stripeSub.CustomerId);
            if (planFree == null)
                return;
            try
            {
                await _suscripcionRepo.CrearAsync(new Suscripcion
                {
                    UsuarioId = user.Id,
                    PlanId = planFree.Id,
                    CurrentPeriodStart = DateTime.UtcNow,
                    CurrentPeriodEnd = DateTime.MaxValue,
                    Status = "active",
                    IsActive = true,
                    AutoRenew = false,
                    CreatedAt = DateTime.UtcNow,
                    StripeCustomerId = "",
                    StripeSubscriptionId = "",
                    StripePriceId = "",
                    StripeCheckoutSessionId = "",
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        

        sub.Status = "canceled";
        sub.IsActive = false;
        sub.CanceledAt = DateTime.UtcNow;
        sub.UpdatedAt = DateTime.UtcNow;

        await _suscripcionRepo.ActualizarAsync(sub);
        if (planFree == null)
            return;
        try
        {
            await _suscripcionRepo.CrearAsync(new Suscripcion
            {
                UsuarioId = sub.UsuarioId,
                PlanId = planFree.Id,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.MaxValue,
                Status = "active",
                IsActive = true,
                AutoRenew = false,
                CreatedAt = DateTime.UtcNow,
                StripeCustomerId = "",
                StripeSubscriptionId = "",
                StripePriceId = "",
                StripeCheckoutSessionId = "",
            });
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
        }

    }
}
