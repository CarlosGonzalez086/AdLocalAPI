using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace AdLocalAPI.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly IStripeWebhookEventRepository _webhookEventRepo;
        private readonly ISuscripcionRepository _suscripcionRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IPlanRepository _planRepo;
        private readonly ILogger<WebhookService> _logger;
        private readonly string _webhookSecret;

        public WebhookService(
            IStripeWebhookEventRepository webhookEventRepo,
            ISuscripcionRepository suscripcionRepo,
            IUsuarioRepository usuarioRepo,
            IPlanRepository planRepo,
            IConfiguration config,
            ILogger<WebhookService> logger)
        {
            _webhookEventRepo = webhookEventRepo;
            _suscripcionRepo = suscripcionRepo;
            _usuarioRepo = usuarioRepo;
            _planRepo = planRepo;
            _logger = logger;
            _webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                ?? config["Stripe:WebhookSecret"]
                ?? string.Empty;
        }

        public async Task<ApiResponse<object>> ProcesarWebhookStripeAsync(string jsonPayload, string? signatureHeader, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(_webhookSecret))
            {
                _logger.LogError("Stripe Webhook Secret no está configurado.");
                return ApiResponse<object>.Error("500", "Stripe Webhook Secret no configurado en el servidor.");
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    jsonPayload,
                    signatureHeader,
                    _webhookSecret
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firma de webhook de Stripe inválida o cuerpo mal formado.");
                return ApiResponse<object>.Error("400", "Firma de Stripe inválida.");
            }

            // 1. Idempotencia: Verificar si el evento ya fue registrado/procesado
            var eventoExistente = await _webhookEventRepo.ObtenerPorEventIdAsync(stripeEvent.Id);

            if (eventoExistente != null && eventoExistente.Status == "processed")
            {
                _logger.LogInformation("Evento Stripe {EventId} [{EventType}] ya procesado previamente. Ignorando.", stripeEvent.Id, stripeEvent.Type);
                return ApiResponse<object>.Success(new { status = "already_processed" }, "Evento ya procesado previamente.");
            }

            if (eventoExistente == null)
            {
                eventoExistente = new StripeWebhookEvent
                {
                    StripeEventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    Status = "processing",
                    ReceivedAt = DateTime.UtcNow
                };
                await _webhookEventRepo.CrearAsync(eventoExistente);
            }

            // 2. Procesamiento transaccional
            await using var transaction = await _webhookEventRepo.IniciarTransaccionAsync();
            try
            {
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await OnCheckoutCompleted((Stripe.Checkout.Session)stripeEvent.Data.Object);
                        break;

                    case "invoice.payment_succeeded":
                        await OnInvoicePaymentSucceeded((Invoice)stripeEvent.Data.Object);
                        break;

                    case "invoice.payment_failed":
                        await OnInvoicePaymentFailed((Invoice)stripeEvent.Data.Object);
                        break;

                    case "customer.subscription.updated":
                        await OnSubscriptionUpdated((Subscription)stripeEvent.Data.Object);
                        break;

                    case "customer.subscription.deleted":
                        await OnSubscriptionDeleted((Subscription)stripeEvent.Data.Object);
                        break;

                    default:
                        _logger.LogInformation("Evento Stripe no manejado: {EventType}", stripeEvent.Type);
                        break;
                }

                eventoExistente.Status = "processed";
                eventoExistente.ProcessedAt = DateTime.UtcNow;
                eventoExistente.ErrorMessage = null;
                await _webhookEventRepo.ActualizarAsync(eventoExistente);

                await transaction.CommitAsync();
                _logger.LogInformation("Evento Stripe {EventId} [{EventType}] procesado correctamente", stripeEvent.Id, stripeEvent.Type);
                return ApiResponse<object>.Success(new { status = "processed" }, "Evento procesado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al procesar evento Stripe {EventId} [{EventType}]", stripeEvent.Id, stripeEvent.Type);

                try
                {
                    eventoExistente.Status = "failed";
                    eventoExistente.ErrorMessage = ex.Message;
                    await _webhookEventRepo.ActualizarAsync(eventoExistente);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Error al registrar fallo de evento Stripe {EventId}", stripeEvent.Id);
                }

                return ApiResponse<object>.Error("500", "Error al procesar webhook.");
            }
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

            if (session.Metadata == null ||
                !session.Metadata.TryGetValue("usuarioId", out var usuarioIdStr) ||
                !long.TryParse(usuarioIdStr, out var usuarioId) ||
                !session.Metadata.TryGetValue("planId", out var planIdStr) ||
                !int.TryParse(planIdStr, out var planId))
            {
                _logger.LogWarning("Sesión checkout {SessionId} no contiene metadata obligatoria (usuarioId, planId)", session.Id);
                return;
            }

            await _suscripcionRepo.CrearAsync(new Suscripcion
            {
                UsuarioId = usuarioId,
                PlanId = planId,
                StripeCustomerId = session.CustomerId ?? string.Empty,
                StripeSubscriptionId = session.SubscriptionId ?? string.Empty,
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

            string? subscriptionId = ExtraerSubscriptionId(invoice);

            if (string.IsNullOrEmpty(subscriptionId))
                return;

            var user = await _usuarioRepo.GetByStripeId(invoice.CustomerId);
            if (user == null)
            {
                _logger.LogWarning("No se encontró usuario para Stripe Customer {CustomerId}", invoice.CustomerId);
                return;
            }

            var subService = new SubscriptionService();
            var stripeSub = await subService.GetAsync(
                subscriptionId,
                new SubscriptionGetOptions
                {
                    Expand = new List<string> { "items.data.price" }
                });

            if (stripeSub?.Items?.Data == null || !stripeSub.Items.Data.Any())
                return;

            var periodStart = stripeSub.Items.Data[0].CurrentPeriodStart;
            var periodEnd = stripeSub.Items.Data[0].CurrentPeriodEnd;
            var priceId = stripeSub.Items.Data[0].Price?.Id ?? string.Empty;

            var sub = await _suscripcionRepo.ObtenerPorStripeId(stripeSub.Id);

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

                var plan = await _planRepo.GetByStripePriceIdAsync(priceId);
                if (plan == null)
                {
                    _logger.LogWarning("No se encontró plan para PriceId {PriceId}", priceId);
                    return;
                }

                await _suscripcionRepo.CrearAsync(new Suscripcion
                {
                    UsuarioId = user.Id,
                    PlanId = plan.Id,
                    StripeCustomerId = invoice.CustomerId ?? string.Empty,
                    StripeSubscriptionId = stripeSub.Id,
                    StripePriceId = priceId,
                    CurrentPeriodStart = periodStart,
                    CurrentPeriodEnd = periodEnd,
                    Status = "active",
                    IsActive = true,
                    AutoRenew = stripeSub.CancelAtPeriodEnd == false,
                    CreatedAt = DateTime.UtcNow
                });

                return;
            }

            sub.CurrentPeriodStart = periodStart;
            sub.CurrentPeriodEnd = periodEnd;
            sub.Status = "active";
            sub.IsActive = true;
            sub.StripePriceId = priceId;
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

            var subscriptionId = ExtraerSubscriptionId(invoice);

            if (string.IsNullOrEmpty(subscriptionId))
                return;

            var sub = await _suscripcionRepo.ObtenerPorStripeId(subscriptionId);
            if (sub == null)
                return;

            sub.Status = "past_due";
            sub.IsActive = true;
            sub.UpdatedAt = DateTime.UtcNow;

            await _suscripcionRepo.ActualizarAsync(sub);
            _logger.LogWarning("Factura fallida para suscripción {SubId}. Marcada past_due.", subscriptionId);
        }

        // =================================================
        // 4️ Cambios de estado
        // =================================================
        private async Task OnSubscriptionUpdated(Subscription stripeSub)
        {
            var sub = await _suscripcionRepo.ObtenerPorStripeId(stripeSub.Id);
            if (sub == null)
                return;

            sub.Status = stripeSub.Status;

            if (stripeSub.CancelAtPeriodEnd)
            {
                sub.Status = "canceling";
                sub.AutoRenew = false;
                sub.CanceledAt = sub.CurrentPeriodEnd ?? DateTime.UtcNow;
            }
            else
            {
                sub.AutoRenew = true;
                sub.CanceledAt = null;
            }

            if (stripeSub.Status == "active" ||
                stripeSub.Status == "trialing" ||
                stripeSub.Status == "past_due" ||
                sub.Status == "canceling")
            {
                sub.IsActive = true;
            }
            else
            {
                sub.IsActive = false;
            }

            if (stripeSub.Items?.Data?.Any() == true && stripeSub.Items.Data[0].Price != null)
            {
                sub.StripePriceId = stripeSub.Items.Data[0].Price.Id;
            }

            sub.UpdatedAt = DateTime.UtcNow;
            await _suscripcionRepo.ActualizarAsync(sub);
            _logger.LogInformation("Suscripción {SubId} actualizada a {Status}", stripeSub.Id, sub.Status);
        }

        // =================================================
        // 5️ Eliminada
        // =================================================
        private async Task OnSubscriptionDeleted(Subscription stripeSub)
        {
            var sub = await _suscripcionRepo.ObtenerPorStripeId(stripeSub.Id);
            var planFree = await _planRepo.GetByTipoAsync("FREE");

            if (sub == null)
            {
                var user = await _usuarioRepo.GetByStripeId(stripeSub.CustomerId);
                if (planFree == null || user == null)
                    return;

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
                    StripeCustomerId = string.Empty,
                    StripeSubscriptionId = string.Empty,
                    StripePriceId = string.Empty,
                    StripeCheckoutSessionId = string.Empty,
                });

                return;
            }

            sub.Status = "canceled";
            sub.IsActive = false;
            sub.CanceledAt = DateTime.UtcNow;
            sub.UpdatedAt = DateTime.UtcNow;

            await _suscripcionRepo.ActualizarAsync(sub);

            if (planFree != null)
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
                    StripeCustomerId = string.Empty,
                    StripeSubscriptionId = string.Empty,
                    StripePriceId = string.Empty,
                    StripeCheckoutSessionId = string.Empty,
                });
            }

            _logger.LogInformation("Suscripción {SubId} cancelada y plan FREE asignado", stripeSub.Id);
        }

        private static string? ExtraerSubscriptionId(Invoice invoice)
        {
            if (invoice?.Lines?.Data == null)
                return null;

            foreach (var line in invoice.Lines.Data)
            {
                if (line.Parent?.SubscriptionItemDetails?.Subscription != null)
                    return line.Parent.SubscriptionItemDetails.Subscription;

                if (!string.IsNullOrEmpty(line.SubscriptionId))
                    return line.SubscriptionId;

                if (!string.IsNullOrEmpty(line.Subscription?.Id))
                    return line.Subscription.Id;
            }

            return null;
        }
    }
}
