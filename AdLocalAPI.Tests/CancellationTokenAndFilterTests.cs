using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Filters;
using AdLocalAPI.Middlewares;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Validators;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class CancellationTokenAndFilterTests
    {
        [Fact]
        public async Task ValidationFilter_DtoInvalido_RetornaBadRequestConApiResponse()
        {
            var filter = new ValidationFilter();
            var httpContext = new DefaultHttpContext();
            var spMock = new Mock<IServiceProvider>();
            spMock.Setup(sp => sp.GetService(typeof(IValidator<LoginDto>)))
                .Returns(new LoginDtoValidator());
            httpContext.RequestServices = spMock.Object;

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary()
            );

            var invalidDto = new LoginDto { Email = "", Password = "" };
            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?> { ["dto"] = invalidDto },
                new object()
            );

            var nextExecuted = false;
            ActionExecutionDelegate next = () =>
            {
                nextExecuted = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
            };

            await filter.OnActionExecutionAsync(actionExecutingContext, next);

            Assert.False(nextExecuted);
            Assert.NotNull(actionExecutingContext.Result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionExecutingContext.Result);
            var response = Assert.IsAssignableFrom<ApiResponse>(badRequest.Value);
            Assert.False(response.EsExitoso);
            Assert.Equal("400", response.Codigo);
        }

        [Fact]
        public async Task ValidationFilter_DtoValido_EjecutaNext()
        {
            var filter = new ValidationFilter();
            var httpContext = new DefaultHttpContext();
            var spMock = new Mock<IServiceProvider>();
            spMock.Setup(sp => sp.GetService(typeof(IValidator<LoginDto>)))
                .Returns(new LoginDtoValidator());
            httpContext.RequestServices = spMock.Object;

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary()
            );

            var validDto = new LoginDto { Email = "admin@adlocal.com", Password = "SecretPassword123" };
            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?> { ["dto"] = validDto },
                new object()
            );

            var nextExecuted = false;
            ActionExecutionDelegate next = () =>
            {
                nextExecuted = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
            };

            await filter.OnActionExecutionAsync(actionExecutingContext, next);

            Assert.True(nextExecuted);
            Assert.Null(actionExecutingContext.Result);
        }

        [Fact]
        public async Task GlobalExceptionMiddleware_PeticionCanceladaPorCliente_Retorna499()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.TraceIdentifier = "trace-test-499";
            context.RequestAborted = cts.Token;

            var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
            var envMock = new Mock<IWebHostEnvironment>();

            RequestDelegate next = _ => throw new OperationCanceledException(cts.Token);

            var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object, envMock.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(499, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(response);
            Assert.Equal("499", response.Codigo);
        }

        [Fact]
        public async Task GlobalExceptionMiddleware_ValidationException_Retorna400()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.TraceIdentifier = "trace-test-val-ex";

            var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
            var envMock = new Mock<IWebHostEnvironment>();

            var validationFailures = new List<ValidationFailure>
            {
                new("Correo", "El formato del correo es inválido.")
            };

            RequestDelegate next = _ => throw new ValidationException(validationFailures);

            var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object, envMock.Object);
            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(response);
            Assert.Equal("400", response.Codigo);
            Assert.Equal("El formato del correo es inválido.", response.Mensaje);
        }

        [Fact]
        public async Task CotizacionService_ConTokenCancelado_PropagaCancelacion()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var repoMock = new Mock<ICotizacionRepository>();
            repoMock.Setup(r => r.ObtenerServicioParaCotizarAsync(It.IsAny<Guid>(), cts.Token))
                .ThrowsAsync(new OperationCanceledException(cts.Token));

            var service = new CotizacionService(repoMock.Object);

            var dto = new CrearCotizacionDto
            {
                ProductoUuid = Guid.NewGuid(),
                Solicitud = "Servicio urgente"
            };

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await service.CrearCotizacionAsync(1, dto, cts.Token);
            });
        }
    }
}
