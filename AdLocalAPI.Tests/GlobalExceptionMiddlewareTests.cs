using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AdLocalAPI.Exceptions;
using AdLocalAPI.Middlewares;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger = new();
        private readonly Mock<IWebHostEnvironment> _mockEnv = new();

        private DefaultHttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.TraceIdentifier = "test-trace-id-12345";
            return context;
        }

        private async Task<ApiResponse<object>?> ReadResponsePayloadAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        [Fact]
        public async Task InvokeAsync_SinExcepcion_EjecutaSiguienteMiddleware()
        {
            var context = CreateHttpContext();
            var siguienteLlamado = false;
            RequestDelegate next = ctx =>
            {
                siguienteLlamado = true;
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.True(siguienteLlamado);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ExcepcionNoControlada_EnProduccion_Retorna500YSanitizado()
        {
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

            var context = CreateHttpContext();
            RequestDelegate next = _ => throw new Exception("Database credentials: password=supersecret at /var/www/internal");

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Contains("application/json", context.Response.ContentType);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("500", payload.Codigo);
            Assert.Contains("error interno en el servidor", payload.Mensaje, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("test-trace-id-12345", payload.Mensaje);
            Assert.DoesNotContain("supersecret", payload.Mensaje);
            Assert.DoesNotContain("/var/www/internal", payload.Mensaje);
            Assert.Null(payload.Respuesta);
        }

        [Fact]
        public async Task InvokeAsync_ExcepcionNoControlada_EnDesarrollo_Retorna500YMensajeTecnico()
        {
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var context = CreateHttpContext();
            var mensajeDetallado = "Detalle técnico para el desarrollador: error en servicio X";
            RequestDelegate next = _ => throw new Exception(mensajeDetallado);

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("500", payload.Codigo);
            Assert.Equal(mensajeDetallado, payload.Mensaje);
            Assert.Null(payload.Respuesta);
        }

        [Fact]
        public async Task InvokeAsync_DomainException_RetornaCodigoPersonalizadoYMensaje()
        {
            var context = CreateHttpContext();
            var mensaje = "Regla de negocio violada: Saldo insuficiente";
            RequestDelegate next = _ => throw new ReglaNegocioException(mensaje);

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("400", payload.Codigo);
            Assert.Equal(mensaje, payload.Mensaje);
            Assert.Null(payload.Respuesta);
        }

        [Fact]
        public async Task InvokeAsync_RecursoNoEncontradoException_Retorna404NotFound()
        {
            var context = CreateHttpContext();
            var mensaje = "Comercio con ID 42 no encontrado";
            RequestDelegate next = _ => throw new RecursoNoEncontradoException(mensaje);

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("404", payload.Codigo);
            Assert.Equal(mensaje, payload.Mensaje);
        }

        [Fact]
        public async Task InvokeAsync_NoAutorizadoException_Retorna401Unauthorized()
        {
            var context = CreateHttpContext();
            var mensaje = "Permiso denegado para esta acción";
            RequestDelegate next = _ => throw new NoAutorizadoException(mensaje);

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("401", payload.Codigo);
            Assert.Equal(mensaje, payload.Mensaje);
        }

        [Fact]
        public async Task InvokeAsync_ConflictoNegocioException_Retorna409Conflict()
        {
            var context = CreateHttpContext();
            var mensaje = "La cita ya fue agendada previamente por otro usuario";
            RequestDelegate next = _ => throw new ConflictoNegocioException(mensaje);

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("409", payload.Codigo);
            Assert.Equal(mensaje, payload.Mensaje);
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Retorna400BadRequest()
        {
            var context = CreateHttpContext();
            RequestDelegate next = _ => throw new ArgumentException("El campo nombre no puede ser nulo o vacío.");

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("400", payload.Codigo);
            Assert.Contains("nombre no puede ser nulo", payload.Mensaje);
        }

        [Fact]
        public async Task InvokeAsync_UnauthorizedAccessException_Retorna401Unauthorized()
        {
            var context = CreateHttpContext();
            RequestDelegate next = _ => throw new UnauthorizedAccessException("Token de autenticación expirado.");

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("401", payload.Codigo);
            Assert.Equal("Token de autenticación expirado.", payload.Mensaje);
        }

        [Fact]
        public async Task InvokeAsync_KeyNotFoundException_Retorna404NotFound()
        {
            var context = CreateHttpContext();
            RequestDelegate next = _ => throw new KeyNotFoundException("No se encontró el elemento con la clave especificada.");

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("404", payload.Codigo);
            Assert.Equal("No se encontró el elemento con la clave especificada.", payload.Mensaje);
        }

        [Fact]
        public async Task InvokeAsync_InvalidOperationException_Retorna400BadRequest()
        {
            var context = CreateHttpContext();
            var mensaje = "Stock insuficiente para 'Camiseta Polo'. Disponible: 0.";
            RequestDelegate next = _ => throw new InvalidOperationException(mensaje);

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("400", payload.Codigo);
            Assert.Equal(mensaje, payload.Mensaje);
        }

        [Fact]
        public async Task InvokeAsync_DbUpdateConcurrencyException_Retorna409Conflict()
        {
            var context = CreateHttpContext();
            RequestDelegate next = _ => throw new DbUpdateConcurrencyException("Conflicto concurrente en base de datos.");

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

            var payload = await ReadResponsePayloadAsync(context);
            Assert.NotNull(payload);
            Assert.Equal("409", payload.Codigo);
            Assert.Contains("concurrente", payload.Mensaje, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task InvokeAsync_VerificaEstructuraJsonExacta_CodigoMensajeRespuesta()
        {
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var context = CreateHttpContext();
            RequestDelegate next = _ => throw new InvalidOperationException("Operación fallida");

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var rawJson = await reader.ReadToEndAsync();

            // Debe contener estrictamente los atributos en camelCase
            Assert.Contains("\"codigo\":", rawJson);
            Assert.Contains("\"mensaje\":", rawJson);
            Assert.Contains("\"respuesta\":", rawJson);
            // No debe contener propiedades no contratadas
            Assert.DoesNotContain("\"esExitoso\":", rawJson);
        }
    }
}
