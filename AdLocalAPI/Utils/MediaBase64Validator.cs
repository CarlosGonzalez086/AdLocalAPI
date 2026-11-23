using System;
using System.Collections.Generic;
using System.Linq;

namespace AdLocalAPI.Utils
{
    public static class MediaBase64Validator
    {
        public const long MaxImageBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp"
        };

        public static bool TryDecodeValidatedImage(
            string? rawInput,
            out byte[] bytes,
            out string contentType,
            out string errorMessage,
            long maxBytes = MaxImageBytes)
        {
            bytes = Array.Empty<byte>();
            contentType = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawInput))
            {
                errorMessage = "No se recibió la imagen";
                return false;
            }

            var trimmed = rawInput.Trim();
            if (!trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Formato de imagen inválido. Se requiere un Data URI válido (JPG, PNG o WEBP).";
                return false;
            }

            var commaIndex = trimmed.IndexOf(',');
            if (commaIndex <= 5)
            {
                errorMessage = "Formato de imagen inválido";
                return false;
            }

            var header = trimmed[5..commaIndex];
            var parts = header.Split(';');
            if (parts.Length < 2 || !parts.Skip(1).Any(p => p.Equals("base64", StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "Formato de imagen inválido";
                return false;
            }

            contentType = parts[0].Trim().ToLowerInvariant();
            if (!AllowedImageTypes.Contains(contentType))
            {
                errorMessage = "Tipo de imagen no permitido. Solo JPEG, JPG, PNG o WEBP";
                return false;
            }

            var base64Payload = trimmed[(commaIndex + 1)..].Trim();
            var maxBase64Chars = ((maxBytes + 2) / 3 * 4) + 8;
            if (base64Payload.Length == 0 || base64Payload.Length > maxBase64Chars)
            {
                errorMessage = $"La imagen excede el tamaño máximo permitido ({maxBytes / (1024 * 1024)} MB).";
                return false;
            }

            try
            {
                bytes = Convert.FromBase64String(base64Payload);
            }
            catch (FormatException)
            {
                errorMessage = "La imagen no contiene un Base64 válido.";
                return false;
            }

            if (bytes.Length == 0 || bytes.LongLength > maxBytes)
            {
                bytes = Array.Empty<byte>();
                errorMessage = $"La imagen excede el tamaño máximo permitido ({maxBytes / (1024 * 1024)} MB).";
                return false;
            }

            if (!HasValidImageSignature(bytes, contentType))
            {
                bytes = Array.Empty<byte>();
                errorMessage = "El contenido binario no corresponde a una imagen JPG, PNG o WEBP válida.";
                return false;
            }

            return true;
        }

        public static bool HasValidImageSignature(byte[] content, string contentType)
        {
            if (content.Length < 4) return false;

            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" =>
                    content[0] == 0xFF &&
                    content[1] == 0xD8 &&
                    content[2] == 0xFF,

                "image/png" =>
                    content.Length >= 8 &&
                    content[0] == 0x89 &&
                    content[1] == 0x50 &&
                    content[2] == 0x4E &&
                    content[3] == 0x47 &&
                    content[4] == 0x0D &&
                    content[5] == 0x0A &&
                    content[6] == 0x1A &&
                    content[7] == 0x0A,

                "image/webp" =>
                    content.Length >= 12 &&
                    content[0] == 0x52 &&
                    content[1] == 0x49 &&
                    content[2] == 0x46 &&
                    content[3] == 0x46 &&
                    content[8] == 0x57 &&
                    content[9] == 0x45 &&
                    content[10] == 0x42 &&
                    content[11] == 0x50,

                _ => false
            };
        }
    }
}
