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

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        // ======================
        // USUARIO
        // ======================

        public int GetUserId()
        {
            var id = User?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(id))
                throw new Exception("No se encontró el ID del usuario en el JWT");

            return int.Parse(id);
        }

        public string GetUserRole()
        {
            return User?.FindFirst("rol")?.Value ?? "";
        }

        public string GetNombre()
        {
            return User?.FindFirst("nombre")?.Value ?? "";
        }

        public string GetEmail()
        {
            return User?.FindFirst(ClaimTypes.Email)?.Value
                ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? "";
        }

        public long GetComercioId()
        {
            var comercio = User?.FindFirst("comercioId")?.Value;
            return string.IsNullOrEmpty(comercio) ? 0 : int.Parse(comercio);
        }

        public string GetFotoUrl()
        {
            return User?.FindFirst("fotoUrl")?.Value ?? "";
        }

        // ======================
        // PLAN / SUSCRIPCIÓN
        // ======================

        public int GetPlanId()
        {
            var value = User?.FindFirst("planId")?.Value;
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }

        public string GetPlanTipo()
        {
            return User?.FindFirst("planTipo")?.Value ?? "FREE";
        }

        public int GetNivelVisibilidad()
        {
            var value = User?.FindFirst("nivelVisibilidad")?.Value;
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }

        public int GetMaxNegocios()
        {
            var value = User?.FindFirst("maxNegocios")?.Value;
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }

        public int GetMaxProductos()
        {
            var value = User?.FindFirst("maxProductos")?.Value;
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }

        public int GetMaxFotos()
        {
            var value = User?.FindFirst("maxFotos")?.Value;
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }

        public bool PermiteCatalogo()
        {
            var value = User?.FindFirst("permiteCatalogo")?.Value;
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }

        public bool TieneAnalytics()
        {
            var value = User?.FindFirst("tieneAnalytics")?.Value;
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }

        public bool TieneBadge()
        {
            return !string.IsNullOrEmpty(GetBadgeTexto());
        }

        public string GetBadgeTexto()
        {
            return User?.FindFirst("badge")?.Value ?? "";
        }
    }
}
