using System;
using Microsoft.AspNetCore.Http;

namespace AdLocalAPI.Exceptions
{
    /// <summary>
    /// Excepción base para errores de negocio o de dominio que pueden exponer su mensaje al cliente de forma segura.
    /// </summary>
    public class DomainException : Exception
    {
        public int StatusCode { get; }
        public string Codigo { get; }

        public DomainException(string mensaje, int statusCode = StatusCodes.Status400BadRequest, string? codigo = null)
            : base(mensaje)
        {
            StatusCode = statusCode;
            Codigo = codigo ?? statusCode.ToString();
        }

        public DomainException(string mensaje, Exception innerException, int statusCode = StatusCodes.Status400BadRequest, string? codigo = null)
            : base(mensaje, innerException)
        {
            StatusCode = statusCode;
            Codigo = codigo ?? statusCode.ToString();
        }
    }

    /// <summary>
    /// Excepción para violaciones de reglas de negocio (HTTP 400 Bad Request).
    /// </summary>
    public class ReglaNegocioException : DomainException
    {
        public ReglaNegocioException(string mensaje, string? codigo = "400")
            : base(mensaje, StatusCodes.Status400BadRequest, codigo)
        {
        }
    }

    /// <summary>
    /// Excepción para recursos no encontrados dentro del dominio (HTTP 404 Not Found).
    /// </summary>
    public class RecursoNoEncontradoException : DomainException
    {
        public RecursoNoEncontradoException(string mensaje = "El recurso solicitado no fue encontrado.", string? codigo = "404")
            : base(mensaje, StatusCodes.Status404NotFound, codigo)
        {
        }
    }

    /// <summary>
    /// Excepción para operaciones no autorizadas dentro del dominio (HTTP 401 Unauthorized).
    /// </summary>
    public class NoAutorizadoException : DomainException
    {
        public NoAutorizadoException(string mensaje = "No autorizado para realizar esta acción.", string? codigo = "401")
            : base(mensaje, StatusCodes.Status401Unauthorized, codigo)
        {
        }
    }

    /// <summary>
    /// Excepción para conflictos de recursos o concurrencia dentro del dominio (HTTP 409 Conflict).
    /// </summary>
    public class ConflictoNegocioException : DomainException
    {
        public ConflictoNegocioException(string mensaje = "Existe un conflicto con el estado actual del recurso.", string? codigo = "409")
            : base(mensaje, StatusCodes.Status409Conflict, codigo)
        {
        }
    }
}
