using System;
using System.Collections.Generic;
using System.Linq;

namespace AdLocalAPI.Helpers
{
    public static class CorsSecurityPolicy
    {
        /// <summary>
        /// Evalúa dinámicamente si un origen HTTP está autorizado para acceder a la API con credenciales (cookies/tokens).
        /// Permite:
        /// 1. Entorno local dinámico (localhost y 127.0.0.1 con cualquier puerto HTTP/HTTPS).
        /// 2. Orígenes explícitos declarados en configuración (appsettings).
        /// 3. Dominios de producción y subdominios de despliegues/preview (*.adlocal.store, *.vercel.app, *.workers.dev).
        /// </summary>
        public static bool IsOriginAllowed(string? origin, IEnumerable<string>? allowedOrigins = null)
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            try
            {
                var uri = new Uri(origin);
                var host = uri.Host.ToLowerInvariant();

                // 1. Entorno de desarrollo local (cualquier puerto en localhost o 127.0.0.1)
                if (host == "localhost" || host == "127.0.0.1")
                    return true;

                // 2. Orígenes explícitos configurados
                if (allowedOrigins != null && allowedOrigins.Any(o => string.Equals(o, origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
                    return true;

                // 3. Dominios oficiales y entornos de preview (adlocal.store, vercel.app, workers.dev)
                if (host == "adlocal.store" ||
                    host.EndsWith(".adlocal.store", StringComparison.OrdinalIgnoreCase) ||
                    host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase) ||
                    host.EndsWith(".workers.dev", StringComparison.OrdinalIgnoreCase))
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
