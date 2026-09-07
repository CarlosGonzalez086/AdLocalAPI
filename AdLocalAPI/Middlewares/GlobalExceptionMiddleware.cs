using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AdLocalAPI.Exceptions;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdLocalAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    exception,
                    "La respuesta HTTP ya ha comenzado su transmisión en {Method} {Path}. No se puede modificar el estado ni enviar el payload de error.",
                    context.Request.Method,
                    context.Request.Path
                );
                return;
            }

            var (statusCode, codigo, mensaje) = DeterminarRespuestaError(context, exception);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var apiResponse = new ApiResponse<object>
            {
                Codigo = codigo,
                Mensaje = mensaje,
                Respuesta = null
            };

            var json = JsonSerializer.Serialize(apiResponse, JsonOptions);
            await context.Response.WriteAsync(json);
        }

        private (int statusCode, string codigo, string mensaje) DeterminarRespuestaError(HttpContext context, Exception exception)
        {
            switch (exception)
            {
                case FluentValidation.ValidationException valEx:
                    var primerErrorVal = valEx.Errors.FirstOrDefault()?.ErrorMessage ?? "Error de validación en la solicitud.";
                    _logger.LogWarning(
                        valEx,
                        "Error de validación FluentValidation en {Method} {Path}: {Mensaje}",
                        context.Request.Method,
                        context.Request.Path,
                        primerErrorVal
                    );
                    return (StatusCodes.Status400BadRequest, "400", primerErrorVal);

                case OperationCanceledException cancelEx when context.RequestAborted.IsCancellationRequested:
                    _logger.LogInformation(
                        "Petición cancelada cooperativamente por el cliente en {Method} {Path}.",
                        context.Request.Method,
                        context.Request.Path
                    );
                    return (499, "499", "La solicitud fue cancelada por el cliente.");

                case DomainException domainEx:
                    _logger.LogWarning(
                        domainEx,
                        "Excepción de dominio capturada en {Method} {Path}: {Mensaje} (Código: {Codigo})",
                        context.Request.Method,
                        context.Request.Path,
                        domainEx.Message,
                        domainEx.Codigo
                    );
                    return (domainEx.StatusCode, domainEx.Codigo, domainEx.Message);

                case ArgumentException argEx:
                    _logger.LogWarning(
                        argEx,
                        "Argumento inválido en {Method} {Path}: {Mensaje}",
                        context.Request.Method,
                        context.Request.Path,
                        argEx.Message
                    );
                    return (StatusCodes.Status400BadRequest, "400", argEx.Message);

                case BadHttpRequestException badHttpEx:
                    _logger.LogWarning(
                        badHttpEx,
                        "Petición HTTP incorrecta en {Method} {Path}: {Mensaje}",
                        context.Request.Method,
                        context.Request.Path,
                        badHttpEx.Message
                    );
                    return (badHttpEx.StatusCode, badHttpEx.StatusCode.ToString(), badHttpEx.Message);

                case UnauthorizedAccessException authEx:
                    _logger.LogWarning(
                        authEx,
                        "Acceso no autorizado en {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path
                    );
                    var mensajeAuth = !string.IsNullOrWhiteSpace(authEx.Message)
                        ? authEx.Message
                        : "No autorizado para realizar esta acción.";
                    return (StatusCodes.Status401Unauthorized, "401", mensajeAuth);

                case KeyNotFoundException notFoundEx:
                    _logger.LogWarning(
                        notFoundEx,
                        "Recurso no encontrado en {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path
                    );
                    var mensajeNotFound = !string.IsNullOrWhiteSpace(notFoundEx.Message)
                        ? notFoundEx.Message
                        : "El recurso solicitado no fue encontrado.";
                    return (StatusCodes.Status404NotFound, "404", mensajeNotFound);

                case DbUpdateConcurrencyException concurrencyEx:
                    _logger.LogWarning(
                        concurrencyEx,
                        "Conflicto de concurrencia detectado en base de datos en {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path
                    );
                    return (
                        StatusCodes.Status409Conflict,
                        "409",
                        "El recurso fue modificado o eliminado por otra operación concurrente. Por favor, recarga y vuelve a intentar."
                    );

                case InvalidOperationException invalidOpEx:
                    _logger.LogWarning(
                        invalidOpEx,
                        "Operación no válida en el estado actual en {Method} {Path}: {Mensaje}",
                        context.Request.Method,
                        context.Request.Path,
                        invalidOpEx.Message
                    );
                    return (StatusCodes.Status400BadRequest, "400", invalidOpEx.Message);

                default:
                    _logger.LogError(
                        exception,
                        "Excepción no controlada capturada procesando {Method} {Path}. TraceId: {TraceId}",
                        context.Request.Method,
                        context.Request.Path,
                        context.TraceIdentifier
                    );

                    // Blindaje de seguridad en producción: jamás exponer stack traces, rutas del filesystem o SQL interno
                    var mensajeInterno = _env.IsDevelopment()
                        ? exception.Message
                        : $"Ha ocurrido un error interno en el servidor. (ID de rastreo: {context.TraceIdentifier})";

                    return (StatusCodes.Status500InternalServerError, "500", mensajeInterno);
            }
        }
    }
}
