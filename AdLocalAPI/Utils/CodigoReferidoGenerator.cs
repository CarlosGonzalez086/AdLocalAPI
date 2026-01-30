using System.Security.Cryptography;
using System.Text;

namespace AdLocalAPI.Utils
{
    public static class CodigoReferidoGenerator
    {
        private const string Prefijo = "ADL-";
        private const string Caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Generar(int longitud = 8)
        {
            if (longitud <= 0)
                throw new ArgumentException("La longitud debe ser mayor a 0");

            var bytes = new byte[longitud];
            RandomNumberGenerator.Fill(bytes);

            var resultado = new StringBuilder(longitud);

            foreach (var b in bytes)
            {
                resultado.Append(Caracteres[b % Caracteres.Length]);
            }

            return Prefijo + resultado.ToString();
        }
    }
}
