using System.Text.Json.Serialization;

namespace AdLocalAPI.Models
{
    public class ApiResponse<T>
    {
        public string Codigo { get; set; } = "200";
        public string Mensaje { get; set; } = string.Empty;
        public T? Respuesta { get; set; }

        [JsonIgnore]
        public bool EsExitoso => Codigo == "200" || Codigo == "201" || Codigo == "204";

        public static ApiResponse<T> Success(T? data = default, string mensaje = "Operación exitosa")
        {
            return new ApiResponse<T>
            {
                Codigo = "200",
                Mensaje = mensaje,
                Respuesta = data
            };
        }

        public static ApiResponse<T> Error(string codigo, string mensaje)
        {
            return new ApiResponse<T>
            {
                Codigo = codigo,
                Mensaje = mensaje,
                Respuesta = default
            };
        }

        public static ApiResponse<T> BadRequest(string mensaje = "Solicitud inválida") => Error("400", mensaje);
        public static ApiResponse<T> Unauthorized(string mensaje = "No autorizado") => Error("401", mensaje);
        public static ApiResponse<T> Forbid(string mensaje = "Acceso denegado") => Error("403", mensaje);
        public static ApiResponse<T> NotFound(string mensaje = "Recurso no encontrado") => Error("404", mensaje);
        public static ApiResponse<T> Conflict(string mensaje = "Conflicto en la operación") => Error("409", mensaje);
        public static ApiResponse<T> InternalServerError(string mensaje = "Error interno del servidor") => Error("500", mensaje);
    }

    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse Success(string mensaje = "Operación exitosa")
        {
            return new ApiResponse
            {
                Codigo = "200",
                Mensaje = mensaje,
                Respuesta = null
            };
        }

        public static new ApiResponse Error(string codigo, string mensaje)
        {
            return new ApiResponse
            {
                Codigo = codigo,
                Mensaje = mensaje,
                Respuesta = null
            };
        }

        public static new ApiResponse BadRequest(string mensaje = "Solicitud inválida") => Error("400", mensaje);
        public static new ApiResponse Unauthorized(string mensaje = "No autorizado") => Error("401", mensaje);
        public static new ApiResponse Forbid(string mensaje = "Acceso denegado") => Error("403", mensaje);
        public static new ApiResponse NotFound(string mensaje = "Recurso no encontrado") => Error("404", mensaje);
        public static new ApiResponse Conflict(string mensaje = "Conflicto en la operación") => Error("409", mensaje);
        public static new ApiResponse InternalServerError(string mensaje = "Error interno del servidor") => Error("500", mensaje);
    }
}
