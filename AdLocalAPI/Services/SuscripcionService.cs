using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Utils;
using Stripe.Checkout;


namespace AdLocalAPI.Services
{
    public class SuscripcionService
    {
        private readonly JwtContext _jwt;
        private readonly UsuarioRepository _usuarioRepository;
        private readonly SuscripcionRepository _suscripcionRepository;
        private readonly PlanRepository _planRepository;
        private readonly StripeServiceSub _stripe;


        public SuscripcionService(
            JwtContext jwt,
            UsuarioRepository usuarioRepository,
            SuscripcionRepository suscripcionRepository,
            PlanRepository planRepository,
            StripeServiceSub stripe)
        {
            _jwt = jwt;
            _usuarioRepository = usuarioRepository;
            _suscripcionRepository = suscripcionRepository;
            _planRepository = planRepository;
            _stripe = stripe;
        }

        public async Task<ApiResponse<string>> CrearCheckoutSession(CrearSuscripcionDto dto)
        {
            int usuarioId = _jwt.GetUserId();

            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            if (usuario == null)
                return ApiResponse<string>.Error("404", "Usuario no encontrado");

            var plan = await _planRepository.GetByIdAsync(dto.PlanId);
            if (plan == null || !plan.Activo)
                return ApiResponse<string>.Error("404", "Plan no válido");

            if (!StripePriceIds.Planes.TryGetValue(plan.Tipo.ToUpper(), out var priceId))
                return ApiResponse<string>.Error("400", "PriceId no configurado");

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                CustomerEmail = usuario.Email,
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                Price = priceId,
                Quantity = 1
            }
        },
                Metadata = new Dictionary<string, string>
        {
            { "usuarioId", usuarioId.ToString() },
            { "planId", plan.Id.ToString() }
        },
                SuccessUrl = "https://tudominio.com/success",
                CancelUrl = "https://tudominio.com/cancel"
            };

            var session = new SessionService().Create(options);

            return ApiResponse<string>.Success(session.Url);
        }
        public async Task<ApiResponse<bool>> CancelarSuscripcion()
        {
            int usuarioId = _jwt.GetUserId();

            var sub = await _suscripcionRepository.ObtenerActiva(usuarioId);
            if (sub == null)
                return ApiResponse<bool>.Error("404", "No tienes suscripción activa");

            _stripe.CancelarSuscripcion(sub.StripeSubscriptionId);

            sub.Activa = false;
            sub.Estado = "canceled";
            sub.FechaCancelacion = DateTime.UtcNow;

            await _suscripcionRepository.ActualizarAsync(sub);

            return ApiResponse<bool>.Success(true, "Suscripción cancelada");
        }

        public async Task<ApiResponse<Suscripcion>> ObtenerSuscripcionActiva()
        {
            int usuarioId = _jwt.GetUserId();

            var sub = await _suscripcionRepository.ObtenerActiva(usuarioId);
            if (sub == null)
                return ApiResponse<Suscripcion>.Error("404", "No tienes suscripción");

            return ApiResponse<Suscripcion>.Success(sub);
        }






        public async Task<ApiResponse<SuscripcionInfoDto>> ObtenerMiSuscripcion()
        {
            int usuarioId = _jwt.GetUserId();

            var suscripcion = await _suscripcionRepository.GetActivaByUsuario(usuarioId);

            var planDto = await _planRepository.GetByIdAsync(suscripcion.PlanId);

            if (suscripcion == null)
                return ApiResponse<SuscripcionInfoDto>.Error("404", "No tienes suscripción activa");

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

                    },

                    FechaInicio = suscripcion.FechaInicio,
                    FechaFin = suscripcion.FechaFin,
                    Activa = suscripcion.Activa,
                    Estado = suscripcion.Estado,
                    Monto = suscripcion.Monto,
                    Moneda = suscripcion.Moneda
                }
            );
        }


        public async Task<ApiResponse<Suscripcion>> CambiarPlan(CambiarPlanDto dto)
        {
            int usuarioId = _jwt.GetUserId();

            var sub = await _suscripcionRepository.ObtenerActiva(usuarioId);
            if (sub == null)
                return ApiResponse<Suscripcion>.Error("404", "No tienes suscripción activa");

            var nuevoPlan = await _planRepository.GetByIdAsync(dto.PlanIdNuevo);
            if (nuevoPlan == null || !nuevoPlan.Activo)
                return ApiResponse<Suscripcion>.Error("404", "Plan no válido");
            var nuevoPriceId = ObtenerStripePriceIdPorTipo(nuevoPlan.Tipo);

            _stripe.CambiarPlan(
                sub.StripeSubscriptionId,
                nuevoPriceId
            );

            // BD
            sub.PlanId = nuevoPlan.Id;
            sub.StripePriceId = nuevoPriceId;
            sub.Monto = nuevoPlan.Precio;

            await _suscripcionRepository.ActualizarAsync(sub);

            return ApiResponse<Suscripcion>.Success(sub, "Plan actualizado");
        }
        private string ObtenerStripePriceIdPorTipo(string tipo)
        {
            if (!StripePriceIds.Planes.TryGetValue(tipo.ToUpper(), out var priceId))
                throw new Exception($"No existe PriceId para el plan {tipo}");

            return priceId;
        }
    }
}
