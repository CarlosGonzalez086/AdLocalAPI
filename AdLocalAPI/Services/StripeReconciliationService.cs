using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Stripe;

namespace AdLocalAPI.Services
{
    public class StripeReconciliationService : IStripeReconciliationService
    {
        private readonly ISuscripcionRepository _suscripcionRepo;
        private readonly IPlanRepository _planRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly ISuscripcionService _suscripcionService;
        private readonly ILogger<StripeReconciliationService> _logger;

        public StripeReconciliationService(
            ISuscripcionRepository suscripcionRepo,
            IPlanRepository planRepo,
            IUsuarioRepository usuarioRepo,
            ISuscripcionService suscripcionService,
            ILogger<StripeReconciliationService> logger)
        {
            _suscripcionRepo = suscripcionRepo;
            _planRepo = planRepo;
            _usuarioRepo = usuarioRepo;
            _suscripcionService = suscripcionService;
            _logger = logger;
        }

        public async Task<ApiResponse<SuscripcionInfoDto>> ReconciliarUsuarioAsync(long usuarioId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var usuario = await _usuarioRepo.GetByIdAsync(usuarioId);
                if (usuario == null)
                    return ApiResponse<SuscripcionInfoDto>.Error("404", "Usuario no encontrado");

                var sub = await _suscripcionRepo.GetActivaByUsuario(usuarioId);

                if (sub == null && string.IsNullOrWhiteSpace(usuario.StripeCustomerId))
                    return ApiResponse<SuscripcionInfoDto>.Error("404", "No se encontró información de suscripción para reconciliar");

                if (string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey))
                {
                    _logger.LogWarning("Stripe API Key no configurada, omitiendo sincronización remota para usuario {UsuarioId}", usuarioId);
                    return await _suscripcionService.ObtenerMiSuscripcion();
                }

                // Si no tiene suscripción activa o no tiene StripeSubscriptionId pero tiene StripeCustomerId
                if ((sub == null || string.IsNullOrWhiteSpace(sub.StripeSubscriptionId)) &&
                    !string.IsNullOrWhiteSpace(usuario.StripeCustomerId))
                {
                    var stripeService = new SubscriptionService();
                    var listOptions = new SubscriptionListOptions
                    {
                        Customer = usuario.StripeCustomerId,
                        Status = "all",
                        Limit = 3
                    };

                    var stripeSubs = await stripeService.ListAsync(listOptions);
                    var activeStripeSub = stripeSubs.FirstOrDefault(s => s.Status == "active" || s.Status == "trialing");

                    if (activeStripeSub != null)
                    {
                        var firstItem = activeStripeSub.Items?.Data?.FirstOrDefault();
                        var priceId = firstItem?.Price?.Id ?? string.Empty;
                        var plan = await _planRepo.GetByStripePriceIdAsync(priceId);

                        if (plan != null)
                        {
                            if (sub != null)
                            {
                                sub.IsActive = false;
                                sub.Status = "canceled";
                                sub.UpdatedAt = DateTime.UtcNow;
                                await _suscripcionRepo.ActualizarAsync(sub);
                            }

                            sub = new Suscripcion
                            {
                                UsuarioId = usuarioId,
                                PlanId = plan.Id,
                                StripeCustomerId = usuario.StripeCustomerId,
                                StripeSubscriptionId = activeStripeSub.Id,
                                StripePriceId = priceId,
                                CurrentPeriodStart = firstItem?.CurrentPeriodStart ?? DateTime.UtcNow,
                                CurrentPeriodEnd = firstItem?.CurrentPeriodEnd ?? DateTime.UtcNow.AddMonths(1),
                                Status = activeStripeSub.Status,
                                IsActive = true,
                                AutoRenew = !activeStripeSub.CancelAtPeriodEnd,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _suscripcionRepo.CrearAsync(sub);
                            _logger.LogInformation("Suscripción Stripe {StripeSubId} recuperada y sincronizada para usuario {UsuarioId}", activeStripeSub.Id, usuarioId);
                        }
                    }
                }
                else if (sub != null && !string.IsNullOrWhiteSpace(sub.StripeSubscriptionId))
                {
                    var stripeService = new SubscriptionService();
                    var stripeSub = await stripeService.GetAsync(
                        sub.StripeSubscriptionId,
                        new SubscriptionGetOptions
                        {
                            Expand = new List<string> { "items.data.price" }
                        });

                    if (stripeSub != null && stripeSub.Items?.Data?.Any() == true)
                    {
                        var periodStart = stripeSub.Items.Data[0].CurrentPeriodStart;
                        var periodEnd = stripeSub.Items.Data[0].CurrentPeriodEnd;
                        var priceId = stripeSub.Items.Data[0].Price?.Id ?? sub.StripePriceId;

                        bool changed = false;

                        if (sub.Status != stripeSub.Status) { sub.Status = stripeSub.Status; changed = true; }
                        if (sub.CurrentPeriodStart != periodStart) { sub.CurrentPeriodStart = periodStart; changed = true; }
                        if (sub.CurrentPeriodEnd != periodEnd) { sub.CurrentPeriodEnd = periodEnd; changed = true; }
                        if (sub.StripePriceId != priceId) { sub.StripePriceId = priceId; changed = true; }

                        bool shouldBeActive = stripeSub.Status == "active" || stripeSub.Status == "trialing" || stripeSub.Status == "past_due";

                        if (stripeSub.CancelAtPeriodEnd)
                        {
                            if (sub.Status != "canceling") { sub.Status = "canceling"; changed = true; }
                            if (sub.AutoRenew) { sub.AutoRenew = false; changed = true; }
                        }
                        else
                        {
                            if (!sub.AutoRenew && shouldBeActive) { sub.AutoRenew = true; changed = true; }
                        }

                        if (sub.IsActive != shouldBeActive) { sub.IsActive = shouldBeActive; changed = true; }

                        if (changed)
                        {
                            sub.UpdatedAt = DateTime.UtcNow;
                            await _suscripcionRepo.ActualizarAsync(sub);
                            _logger.LogInformation("Suscripción local {SubId} sincronizada con Stripe. Estado: {Status}", sub.Id, sub.Status);
                        }
                    }
                }

                return await _suscripcionService.ObtenerMiSuscripcion();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la reconciliación de suscripción para usuario {UsuarioId}", usuarioId);
                return ApiResponse<SuscripcionInfoDto>.Error("500", $"Error al reconciliar suscripción: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ReconciliacionResumenDto>> ReconciliarTodasAsync(CancellationToken cancellationToken = default)
        {
            var resumen = new ReconciliacionResumenDto();
            var stripeService = new SubscriptionService();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey))
                {
                    _logger.LogWarning("Stripe API Key no configurada, omitiendo sincronización remota masiva con Stripe.");
                    return ApiResponse<ReconciliacionResumenDto>.Success(resumen, "Reconciliación omitida: Stripe API Key no configurada");
                }

                var subs = await _suscripcionRepo.ObtenerActivasConStripeAsync();

                resumen.TotalRevisadas = subs.Count;

                foreach (var sub in subs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var stripeSub = await stripeService.GetAsync(
                            sub.StripeSubscriptionId,
                            new SubscriptionGetOptions
                            {
                                Expand = new List<string> { "items.data.price" }
                            });

                        if (stripeSub == null || stripeSub.Items?.Data == null || !stripeSub.Items.Data.Any())
                        {
                            continue;
                        }

                        bool changed = false;
                        var periodStart = stripeSub.Items.Data[0].CurrentPeriodStart;
                        var periodEnd = stripeSub.Items.Data[0].CurrentPeriodEnd;
                        var priceId = stripeSub.Items.Data[0].Price?.Id ?? sub.StripePriceId;

                        if (sub.Status != stripeSub.Status) { sub.Status = stripeSub.Status; changed = true; }
                        if (sub.CurrentPeriodStart != periodStart) { sub.CurrentPeriodStart = periodStart; changed = true; }
                        if (sub.CurrentPeriodEnd != periodEnd) { sub.CurrentPeriodEnd = periodEnd; changed = true; }
                        if (sub.StripePriceId != priceId) { sub.StripePriceId = priceId; changed = true; }

                        bool shouldBeActive = stripeSub.Status == "active" || stripeSub.Status == "trialing" || stripeSub.Status == "past_due";

                        if (stripeSub.CancelAtPeriodEnd)
                        {
                            if (sub.Status != "canceling") { sub.Status = "canceling"; changed = true; }
                            if (sub.AutoRenew) { sub.AutoRenew = false; changed = true; }
                        }
                        else
                        {
                            if (!sub.AutoRenew && shouldBeActive) { sub.AutoRenew = true; changed = true; }
                        }

                        if (sub.IsActive != shouldBeActive) { sub.IsActive = shouldBeActive; changed = true; }

                        if (changed)
                        {
                            sub.UpdatedAt = DateTime.UtcNow;
                            await _suscripcionRepo.ActualizarAsync(sub);
                            resumen.TotalActualizadas++;
                            resumen.Detalles.Add($"Sub {sub.Id} actualizada a {sub.Status}");
                        }
                    }
                    catch (Exception ex)
                    {
                        resumen.Errores++;
                        resumen.Detalles.Add($"Error en sub {sub.Id} ({sub.StripeSubscriptionId}): {ex.Message}");
                        _logger.LogError(ex, "Error al reconciliar suscripción {SubId}", sub.Id);
                    }
                }

                return ApiResponse<ReconciliacionResumenDto>.Success(resumen, "Reconciliación completada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al ejecutar reconciliación de suscripciones");
                return ApiResponse<ReconciliacionResumenDto>.Error("500", ex.Message);
            }
        }
    }
}
