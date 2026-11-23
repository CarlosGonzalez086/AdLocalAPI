using System;
using System.Collections.Generic;
using System.Linq;

namespace AdLocalAPI.Helpers
{
    public static class CorsSecurityPolicy
    {
        /// <summary>
        /// Evalúa dinámicamente si un origen HTTP está autorizado para acceder a la API con credenciales (cookies/tokens).
        /// Permite únicamente:
        /// 1. Entorno local (localhost y 127.0.0.1 con cualquier puerto HTTP/HTTPS).
        /// 2. Orígenes explícitos declarados en configuración (CORS__ALLOWED_ORIGINS / appsettings).
        /// 3. Dominio oficial y subdominios propios sobre HTTPS (https://adlocal.store y https://*.adlocal.store).
        /// </summary>
        public static bool IsOriginAllowed(string? origin, IEnumerable<string>? allowedOrigins = null)
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            try
            {
                var uri = new Uri(origin);
                var host = uri.Host.ToLowerInvariant();

                // 1. Entorno de desarrollo local (cualquier puerto en localhost o 127.0.0.1)
                if ((uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                    (host == "localhost" || host == "127.0.0.1"))
                    return true;

                // 2. Orígenes explícitos configurados
                if (allowedOrigins != null && allowedOrigins.Any(o => string.Equals(o.TrimEnd('/'), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
                    return true;

                // 3. Dominios oficiales propios únicamente por HTTPS
                if (uri.Scheme == Uri.UriSchemeHttps &&
                    (host == "adlocal.store" || host.EndsWith(".adlocal.store", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
