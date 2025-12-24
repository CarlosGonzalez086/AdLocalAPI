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

        public int GetUserId()
        {
            var id = User?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(id)) throw new Exception("No se encontró el ID del usuario en el JWT");
            return int.Parse(id);
        }

        public string GetUserRole()
        {
            return User?.FindFirst("rol")?.Value ?? "";
        }

        public int? GetComercioId()
        {
            var comercio = User?.FindFirst("comercioId")?.Value;
            if (string.IsNullOrEmpty(comercio)) return null;
            return int.Parse(comercio);
        }

        public string GetEmail()
        {
            return User?.FindFirst(ClaimTypes.Email)?.Value
                   ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "";
        }

        public string GetNombre()
        {
            return User?.FindFirst("nombre")?.Value ?? "";
        }
    }
}
