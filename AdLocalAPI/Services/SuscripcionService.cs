using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Stripe;
using Stripe.Checkout;

namespace AdLocalAPI.Services
{
    public class SuscripcionService : ISuscripcionService
    {
        private readonly JwtContext _jwt;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ISuscripcionRepository _suscripcionRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IWebHostEnvironment _env;

        public SuscripcionService(
            JwtContext jwt,
            IUsuarioRepository usuarioRepository,
            ISuscripcionRepository suscripcionRepository,
            IPlanRepository planRepository,
            IWebHostEnvironment env)
        {
            _jwt = jwt;
            _usuarioRepository = usuarioRepository;
            _suscripcionRepository = suscripcionRepository;
            _planRepository = planRepository;
            _env = env;
        }

        public async Task<ApiResponse<SuscripcionInfoDto>> ObtenerMiSuscripcion()
        {
            long usuarioId = _jwt.GetUserId();

            var suscripcion = await _suscripcionRepository.GetActivaByUsuario(usuarioId);
            if (suscripcion == null)
                return ApiResponse<SuscripcionInfoDto>.Error("404", "No tienes suscripción activa");

            var planDto = await _planRepository.GetByIdAsync(suscripcion.PlanId);
            if (planDto == null)
                return ApiResponse<SuscripcionInfoDto>.Error("404", "Plan de suscripción no encontrado");

            return ApiResponse<SuscripcionInfoDto>.Success(
                new SuscripcionInfoDto
                {
                    Id = suscripcion.Id,
                    Plan = new PlanInfoDto
                    {
                        Nombre = planDto.Nombre,
                        Precio = planDto.Precio,
                        DuracionDias = planDto.DuracionDias,
                        Tipo = planDto.Tipo,
                        MaxNegocios = planDto.MaxNegocios,
                        MaxProductos = planDto.MaxProductos,
                        MaxFotos = planDto.MaxFotos,
                        NivelVisibilidad = planDto.NivelVisibilidad,
                        PermiteCatalogo = planDto.PermiteCatalogo,
                        ColoresPersonalizados = planDto.ColoresPersonalizados,
                        TieneBadge = planDto.TieneBadge,
                        BadgeTexto = planDto.BadgeTexto,
                        TieneAnalytics = planDto.TieneAnalytics,
                        IsMultiUsuario = planDto.IsMultiUsuario,
                    },
                    FechaInicio = suscripcion.CurrentPeriodStart ?? suscripcion.CreatedAt,
                    AutoRenew = suscripcion.AutoRenew,
                    FechaFin = suscripcion.CurrentPeriodEnd ?? DateTime.UtcNow,
                    Activa = suscripcion.IsActive,
                    Estado = suscripcion.Status,
                    Monto = suscripcion.Plan?.Precio ?? planDto.Precio,
                    Moneda = "MXN"
                }
            );
        }

        public async Task<ApiResponse<object>> ObtenerTodasAsync(
            int page,
            int pageSize)
        {
            var (total, suscripciones) =
                await _suscripcionRepository.ObtenerTodasAsync(page, pageSize);

            var data = suscripciones.Select(s => new SuscripcionListadoDto
            {
                Id = s.Id,
                Estado = s.Status,
                FechaInicio = s.CurrentPeriodStart,
                FechaFin = s.CurrentPeriodEnd,
                AutoRenew = s.AutoRenew,
                UsuarioNombre = s.Usuario.Nombre,
                UsuarioEmail = s.Usuario.Email,
                PlanNombre = s.Plan.Nombre,
                PlanTipo = s.Plan.Tipo,
                Precio = s.Plan.Precio
            }).ToList();

            return ApiResponse<object>.Success(new
            {
                totalRecords = total,
                page,
                pageSize,
                data
            });
        }

        public async Task<ApiResponse<SuscripcionDashboardDto>> ObtenerStatsSuscripciones()
        {
            var porPlan = await _suscripcionRepository.ObtenerConteoPorPlan();
            var ultimaSemana = await _suscripcionRepository.SuscripcionesUltimaSemana();
            var ultimosTresMeses = await _suscripcionRepository.SuscripcionesUltimosTresMeses();

            return ApiResponse<SuscripcionDashboardDto>.Success(
                new SuscripcionDashboardDto
                {
                    PorPlan = porPlan,
                    UltimaSemana = ultimaSemana,
                    UltimosTresMeses = ultimosTresMeses
                }
            );
        }

        // =========================
        // SUSCRIPCIÓN CON TARJETA
        // =========================
        public async Task<ApiResponse<string>> SuscribirseConTarjeta(
            int planId,
            string paymentMethodId,
            bool autoRenew)
        {
            var plan = await _planRepository.GetByIdAsync(planId);

            if (plan == null || !plan.Activo)
                return ApiResponse<string>.Error("404", "Plan no encontrado");

            var usuario = await _usuarioRepository.GetByIdAsync(_jwt.GetUserId());
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
                await _usuarioRepository.UpdateAsync(usuario);
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
            var plan = await _planRepository.GetByIdAsync(planId);

            if (plan == null || !plan.Activo)
                return ApiResponse<string>.Error("404", "Plan no encontrado");

            var usuario = await _usuarioRepository.GetByIdAsync(_jwt.GetUserId());
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
                await _usuarioRepository.UpdateAsync(usuario);
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

            var suscripcion = await _suscripcionRepository.GetActivaByUsuarioAsync(usuarioId);

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
}
