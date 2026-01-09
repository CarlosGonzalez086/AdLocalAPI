using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;

namespace AdLocalAPI.Services
{
    public class SuscripcionService
    {
        private readonly SuscripcionRepository _repository;
        private readonly PlanRepository _planRepo;
        private readonly JwtContext _jwt;
        private readonly StripeService _stripe;

        public SuscripcionService(
            SuscripcionRepository repository,
            PlanRepository planRepo,
            JwtContext jwt,
            StripeService stripe)
        {
            _repository = repository;
            _planRepo = planRepo;
            _jwt = jwt;
            _stripe = stripe;
        }

        public async Task<ApiResponse<object>> ContratarPlan(SuscripcionCreateDto dto)
        {
            int usuarioId = _jwt.GetUserId();

            var plan = await _planRepo.GetByIdAsync(dto.PlanId);
            if (plan == null || !plan.Activo)
                return ApiResponse<object>.Error("404", "Plan no disponible");

            var paymentIntent = await _stripe.CreatePaymentIntent(
                plan,
                usuarioId,
                dto.StripePaymentMethodId
            );

            return ApiResponse<object>.Success(
                new
                {
                    paymentIntentId = paymentIntent.Id,
                    status = paymentIntent.Status
                },
                "Pago procesado"
            );
        }


        public async Task<ApiResponse<object>> ObtenerMiSuscripcion()
        {
            int usuarioId = _jwt.GetUserId();
            var suscripcion = await _repository.GetActivaByUsuario(usuarioId);

            if (suscripcion == null)
                return ApiResponse<object>.Error("404", "No tienes suscripción activa");

            return ApiResponse<object>.Success(
                new SuscripcionInfoDto
                {
                    Id = suscripcion.Id,
                    Plan = suscripcion.Plan.Nombre,
                    FechaInicio = suscripcion.FechaInicio,
                    FechaFin = suscripcion.FechaFin,
                    Activa = suscripcion.Activa,
                    Estado = suscripcion.Estado,
                    Monto = suscripcion.Monto,
                    Tipo = "",
                    Moneda = suscripcion.Moneda,
                }
            );
        }

        public async Task<ApiResponse<object>> CancelarMiSuscripcion()
        {
            int usuarioId = _jwt.GetUserId();
            var suscripcion = await _repository.GetActivaByUsuario(usuarioId);

            if (suscripcion == null)
                return ApiResponse<object>.Error("404", "No hay suscripción activa");

            suscripcion.AutoRenovacion = false;
            suscripcion.Estado = "Cancelada";

            await _repository.UpdateAsync(suscripcion);

            return ApiResponse<object>.Success(
                null,
                "La suscripción se cancelará al finalizar el periodo actual"
            );
        }
        public async Task<bool> TieneSuscripcionActiva(int usuarioId)
        {
            var sub = await _repository.GetByUsuarioId(usuarioId);
            return sub != null && sub.FechaFin > DateTime.UtcNow;
        }

    }
}
