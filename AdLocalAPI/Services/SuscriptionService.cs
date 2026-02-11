using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

public class SuscriptionService : ISuscriptionServiceV1
{
    private readonly AppDbContext _context;
    private readonly UsuarioRepository _usuarioRepo;
    private readonly JwtContext _jwt;
    private readonly IWebHostEnvironment _env;

    public SuscriptionService(
        AppDbContext context,
        JwtContext jwt,
        UsuarioRepository usuarioRepo,
        IWebHostEnvironment env)
    {
        _context = context;
        _jwt = jwt;
        _usuarioRepo = usuarioRepo;
        _env = env;
    }

    // =========================
    // SUSCRIPCIÓN CON TARJETA
    // =========================
    public async Task<ApiResponse<string>> SuscribirseConTarjeta(
        int planId,
        string paymentMethodId,
        bool autoRenew)
    {
        var plan = await _context.Plans
            .FirstOrDefaultAsync(p => p.Id == planId && p.Activo);

        if (plan == null)
            return ApiResponse<string>.Error("404", "Plan no encontrado");

        var usuario = await _usuarioRepo.GetByIdAsync(_jwt.GetUserId());
        if (usuario == null)
            return ApiResponse<string>.Error("404", "Usuario no encontrado");

        // 1️ Crear customer si no existe
        if (string.IsNullOrEmpty(usuario.StripeCustomerId))
        {
            var customer = await new CustomerService().CreateAsync(
                new CustomerCreateOptions
                {
                    Email = usuario.Email,
                    Name = usuario.Nombre
                });

            usuario.StripeCustomerId = customer.Id;
            await _usuarioRepo.UpdateAsync(usuario);
        }

        // 2️ Asociar payment method
        await new PaymentMethodService().AttachAsync(
            paymentMethodId,
            new PaymentMethodAttachOptions
            {
                Customer = usuario.StripeCustomerId
            });

        await new CustomerService().UpdateAsync(
            usuario.StripeCustomerId,
            new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            });

        // 3️ Crear suscripción (Stripe controla el tiempo)
        var options = new SubscriptionCreateOptions
        {
            Customer = usuario.StripeCustomerId,
            Items = new()
        {
            new SubscriptionItemOptions
            {
                Price = plan.StripePriceId
            }
        },
            DefaultPaymentMethod = paymentMethodId,
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            },
            ProrationBehavior = "none",
            CancelAtPeriodEnd = !autoRenew,
                            Metadata = new()
                {
                    { "usuarioId", usuario.Id.ToString() },
                    { "days", "30" }
                }
        };

        var subscription = await new SubscriptionService()
            .CreateAsync(options);


        // Espera a invoice.payment_succeeded

        return ApiResponse<string>.Success(
            subscription.Id,
            "Suscripción creada correctamente"
        );
    }


    // =========================
    // CHECKOUT (TARJETA NUEVA)
    // =========================
    public async Task<ApiResponse<string>> CrearCheckoutSuscripcion(int planId)
    {
        var plan = await _context.Plans
            .FirstOrDefaultAsync(p => p.Id == planId && p.Activo);

        if (plan == null)
            return ApiResponse<string>.Error("404", "Plan no encontrado");

        var usuario = await _usuarioRepo.GetByIdAsync(_jwt.GetUserId());
        if (usuario == null)
            return ApiResponse<string>.Error("404", "Usuario no encontrado");

        if (string.IsNullOrEmpty(usuario.StripeCustomerId))
        {
            var customer = await new CustomerService().CreateAsync(
                new CustomerCreateOptions
                {
                    Email = usuario.Email
                });

            usuario.StripeCustomerId = customer.Id;
            await _usuarioRepo.UpdateAsync(usuario);
        }

        string successUrl = _env.IsProduction()
            ? "https://ad-local-gamma.vercel.app/app/checkout/success"
            : "http://localhost:5173/app/checkout/success";

        string cancelUrl = _env.IsProduction()
            ? "https://ad-local-gamma.vercel.app/app/checkout/cancel"
            : "http://localhost:5173/app/checkout/cancel";

        var session = await new SessionService().CreateAsync(
            new SessionCreateOptions
            {
                Mode = "subscription",
                Customer = usuario.StripeCustomerId,
                PaymentMethodTypes = new() { "card" },
                LineItems = new()
                {
                    new SessionLineItemOptions
                    {
                        Price = plan.StripePriceId,
                        Quantity = 1
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,                
                Metadata = new()
                {
                    { "usuarioId", usuario.Id.ToString() },
                    { "planId", plan.Id.ToString() },
                    { "autoRenew", "false" },
                    { "days", "30" }
                }
            });

        return ApiResponse<string>.Success(session.Url!, "Checkout creado");
    }

    // =========================
    // CANCELAR PLAN
    // =========================
    public async Task<ApiResponse<string>> CancelarPlan()
    {
        var usuarioId = _jwt.GetUserId();

        var suscripcion = await _context.Suscripcions
            .FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.IsActive);

        if (suscripcion == null)
            return ApiResponse<string>.Error("404", "No hay suscripción activa");

        await new SubscriptionService().UpdateAsync(
            suscripcion.StripeSubscriptionId,
            new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });

        return ApiResponse<string>.Success(
            "ok",
            "La suscripción se cancelará al final del período"
        );
    }
}
