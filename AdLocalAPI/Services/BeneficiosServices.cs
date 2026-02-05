using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using Stripe;
using Plan = AdLocalAPI.Models.Plan;

namespace AdLocalAPI.Services
{
    public class BeneficiosServices
    {
        private readonly JwtContext _jwt;
        private readonly UsuarioRepository _usuarioRepository;
        private readonly SuscripcionRepository _suscripcionRepository;
        private readonly PlanRepository _planRepository;
        private readonly UsoCodigoReferidoRepository _codigoReferidoRepository;
        public BeneficiosServices(
            JwtContext jwt,
            UsuarioRepository usuarioRepository,
            SuscripcionRepository suscripcionRepository,
                        UsoCodigoReferidoRepository codigoReferidoRepository,
            PlanRepository planRepository)
        {
            _jwt = jwt;
            _usuarioRepository = usuarioRepository;
            _suscripcionRepository = suscripcionRepository;
            _planRepository = planRepository;
            _codigoReferidoRepository = codigoReferidoRepository;
        }

        public async Task<ApiResponse<object>> ReclamarBeneficio()
        {
            long idUser = _jwt.GetUserId();

            var usuario = await _usuarioRepository.GetByIdAsync(idUser);
            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");

            if (string.IsNullOrWhiteSpace(usuario.CodigoReferido))
                return ApiResponse<object>.Error(
                    "400",
                    "El usuario no cuenta con un código de referido válido"
                );

            int totalUsos = await _codigoReferidoRepository
                .ContarPorCodigoAsync(usuario.CodigoReferido);

            if (totalUsos < 15)
                return ApiResponse<object>.Error(
                    "400",
                    $"Beneficio no disponible. Referidos actuales: {totalUsos} / 15"
                );

            var subscription = await _suscripcionRepository
                .GetActivaByUsuarioAsync(idUser);

            if (subscription == null || subscription.Plan.Tipo == "FREE")
            {
                await CrearPlanBasicoGratis(usuario);

                return ApiResponse<object>.Success(
                    null,
                    "Beneficio aplicado correctamente. Se activó el plan básico gratuito"
                );
            }

            await ExtenderSuscripcion(subscription);

            return ApiResponse<object>.Success(
                null,
                "Beneficio aplicado correctamente. La suscripción fue extendida"
            );
        }
        private async Task CrearPlanBasicoGratis(Usuario usuario)
        {
            Plan plan = await _planRepository.GetByTipoAsync("BASIC");
            var options = new SubscriptionCreateOptions
            {
                Customer = usuario.StripeCustomerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = plan.StripePriceId,
                    }
                },
                TrialPeriodDays = 30,
                Metadata = new Dictionary<string, string>
                {
                    { "motivo", "beneficio_referidos" }
                }
            };

            var service = new SubscriptionService();
            var subscription = await service.CreateAsync(options);
        }
        private async Task ExtenderSuscripcion(Suscripcion subscription)
        {
            var options = new SubscriptionUpdateOptions
            {
                ProrationBehavior = "none",
                TrialEnd = subscription.CurrentPeriodEnd!.Value.AddDays(30), 
                Metadata = new Dictionary<string, string>
                {
                    { "beneficio", "referidos_15" },
                    { "extension", "30_dias" },
                }
            };

            var service = new SubscriptionService();
            await service.UpdateAsync(subscription.StripeSubscriptionId, options);
        }


    }
}
