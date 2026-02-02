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



        public SuscripcionService(
            JwtContext jwt,
            UsuarioRepository usuarioRepository,
            SuscripcionRepository suscripcionRepository,
            PlanRepository planRepository)
        {
            _jwt = jwt;
            _usuarioRepository = usuarioRepository;
            _suscripcionRepository = suscripcionRepository;
            _planRepository = planRepository;
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
                        IsMultiUsuario = planDto.IsMultiUsuario,

                    },

                    FechaInicio = (DateTime)suscripcion.CurrentPeriodStart,
                    FechaFin = (DateTime)suscripcion.CurrentPeriodEnd,
                    Activa = suscripcion.IsActive,
                    Estado = suscripcion.Status,
                    Monto = suscripcion.Plan.Precio,
                    Moneda = "MXN"
                }
            );
        }
    }
}
