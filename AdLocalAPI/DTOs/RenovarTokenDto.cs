namespace AdLocalAPI.DTOs
{
    public class RenovarTokenDto
    {
        /// <summary>
        /// Token JWT expirado o token de refresco provisto por el cliente
        /// </summary>
        public string? TokenActual { get; set; }

        /// <summary>
        /// Alias común enviado por clientes frontend
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Token de refresco explícito si no se utilizan cookies
        /// </summary>
        public string? RefreshToken { get; set; }
    }
}
