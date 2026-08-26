namespace AdLocalAPI.Utils
{
    public class UrlHelper
    {
        public static string GenerarLinkCambioPassword(string token, bool esProduccion, string userType)
        {
            var baseUrl = esProduccion
                ? "https://adlocal.jcarlosgonzalez086.workers.dev/usuario/"
                : "http://localhost:5173/usuario";

            return $"{baseUrl}/restablecer-contrasena/{token}/{userType}";
        }
        public static string GenerarLinkNuevoColaborador(string token, bool esProduccion)
        {
            var baseUrl = esProduccion
                ? "https://adlocal.jcarlosgonzalez086.workers.dev/usuario/"
                : "http://localhost:5173/usuario";

            return $"{baseUrl}/nuevo-colaborador/{token}";
        }
    }
}
