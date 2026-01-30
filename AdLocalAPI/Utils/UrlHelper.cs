namespace AdLocalAPI.Utils
{
    public class UrlHelper
    {
        public static string GenerarLinkCambioPassword(string token, bool esProduccion)
        {
            var baseUrl = esProduccion
                ? "https://ad-local-gamma.vercel.app"
                : "http://localhost:5173";

            return $"{baseUrl}/cambiar-contrasena/{token}";
        }
        public static string GenerarLinkNuevoColaborador(string token, bool esProduccion)
        {
            var baseUrl = esProduccion
                ? "https://ad-local-gamma.vercel.app"
                : "http://localhost:5173";

            return $"{baseUrl}/nuevo-colaborador/{token}";
        }
    }
}
