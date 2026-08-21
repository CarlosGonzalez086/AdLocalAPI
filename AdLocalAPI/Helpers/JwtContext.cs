using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace AdLocalAPI.Helpers
{
    public class JwtContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User =>
            _httpContextAccessor.HttpContext?.User;

        // ======================
        // USUARIO
        // ======================

        public long GetUserId()
        {
            var value = User?.FindFirst("id")?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new UnauthorizedAccessException(
                    "No se encontró el ID del usuario en el JWT."
                );
            }

            if (!long.TryParse(value, out var idUsuario))
            {
                throw new UnauthorizedAccessException(
                    "El ID del usuario contenido en el JWT no es válido."
                );
            }

            return idUsuario;
        }

        public string GetUserRole()
        {
            return User?.FindFirst("rol")?.Value
                ?? User?.FindFirst(ClaimTypes.Role)?.Value
                ?? string.Empty;
        }

        public string GetNombre()
        {
            return User?.FindFirst("nombre")?.Value
                ?? string.Empty;
        }

        public string GetEmail()
        {
            return User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? User?.FindFirst(ClaimTypes.Email)?.Value
                ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? string.Empty;
        }

        public string GetFotoUrl()
        {
            return User?.FindFirst("fotoUrl")?.Value
                ?? string.Empty;
        }

        // ======================
        // COMERCIO
        // ======================

        public long GetComercioId()
        {
            var value = User?.FindFirst("comercioId")?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return long.TryParse(value, out var idComercio)
                ? idComercio
                : 0;
        }

        // ======================
        // PLAN / SUSCRIPCIÓN
        // ======================

        public long GetPlanId()
        {
            var value = User?.FindFirst("planId")?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return long.TryParse(value, out var idPlan)
                ? idPlan
                : 0;
        }

        public string GetPlanTipo()
        {
            return User?.FindFirst("planTipo")?.Value
                ?? "FREE";
        }

        public int GetNivelVisibilidad()
        {
            var value = User?.FindFirst("nivelVisibilidad")?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return int.TryParse(value, out var nivel)
                ? nivel
                : 0;
        }

        public int GetMaxNegocios()
        {
            var value = User?.FindFirst("maxNegocios")?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return int.TryParse(value, out var maxNegocios)
                ? maxNegocios
                : 0;
        }

        public int GetMaxProductos()
        {
            var value = User?.FindFirst("maxProductos")?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return int.TryParse(value, out var maxProductos)
                ? maxProductos
                : 0;
        }

        public int GetMaxFotos()
        {
            var value = User?.FindFirst("maxFotos")?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return int.TryParse(value, out var maxFotos)
                ? maxFotos
                : 0;
        }

        public bool PermiteCatalogo()
        {
            var value = User?.FindFirst("permiteCatalogo")?.Value;

            return bool.TryParse(value, out var resultado)
                && resultado;
        }

        public bool TieneAnalytics()
        {
            var value = User?.FindFirst("tieneAnalytics")?.Value;

            return bool.TryParse(value, out var resultado)
                && resultado;
        }

        public bool TieneBadge()
        {
            return !string.IsNullOrWhiteSpace(GetBadgeTexto());
        }

        public string GetBadgeTexto()
        {
            return User?.FindFirst("badge")?.Value
                ?? string.Empty;
        }

        // ======================
        // VALIDACIONES
        // ======================

        public bool EstaAutenticado()
        {
            return User?.Identity?.IsAuthenticated == true;
        }

        public bool EsRol(string rol)
        {
            return string.Equals(
                GetUserRole(),
                rol,
                StringComparison.OrdinalIgnoreCase
            );
        }

        public bool EsCliente()
        {
            return EsRol("Cliente");
        }

        public bool EsComercio()
        {
            return EsRol("Comercio");
        }

        public bool EsColaborador()
        {
            return EsRol("Colaborador");
        }

        public bool EsAdministrador()
        {
            return EsRol("Administrador");
        }
    }
}