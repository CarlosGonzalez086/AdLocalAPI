using System.Security.Cryptography;
using System.Text;

namespace AdLocalAPI.Utils
{
    public class ServicesGenerals
    {
        public static string GenerarCodigoAlfanumerico(int longitud = 8)
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var resultado = new StringBuilder(longitud);

            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[longitud];

            rng.GetBytes(buffer);

            foreach (var b in buffer)
            {
                resultado.Append(caracteres[b % caracteres.Length]);
            }

            return resultado.ToString();
        }
    }
}
