using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using AdLocalAPI.Controllers;
using AdLocalAPI.Exceptions;
using AdLocalAPI.Middlewares;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class Fase3y4IntegralTests
    {
        private static readonly Assembly ApiAssembly = typeof(ApiControllerBase).Assembly;

        private class TestController : ApiControllerBase
        {
            public IActionResult ProbarResponder<T>(ApiResponse<T>? response) => Responder(response);
        }

        // =========================================================================
        // 1. ARQUITECTURA LIMPIA Y SEPARACIÓN DE CAPAS (FASE 3 & FASE 4)
        // =========================================================================

        [Fact]
        public void SeparacionDeCapas_Controladores_SoloInyectanServiciosOInterfaces()
        {
            var controllers = ApiAssembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(controllers);

            foreach (var controller in controllers)
            {
                var ctors = controller.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                foreach (var ctor in ctors)
                {
                    foreach (var param in ctor.GetParameters())
                    {
                        var paramType = param.ParameterType;
                        var typeName = paramType.Name;

                        // No DbContext
                        Assert.False(
                            typeof(DbContext).IsAssignableFrom(paramType),
                            $"El controlador {controller.Name} inyecta directamente {typeName} (DbContext). Violación de Fase 3."
                        );

                        // No Repositorios
                        var isRepo = typeName.EndsWith("Repository", StringComparison.OrdinalIgnoreCase)
                                     || typeName.EndsWith("Repositorio", StringComparison.OrdinalIgnoreCase);
                        Assert.False(
                            isRepo,
                            $"El controlador {controller.Name} inyecta directamente el repositorio {typeName}. Violación de Fase 3."
                        );
                    }
                }
            }
        }

        [Fact]
        public void Fase3_NingunServicioDeDominio_InyectaAppDbContextDirectamente()
        {
            var services = ApiAssembly.GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.Namespace != null
                            && t.Namespace.StartsWith("AdLocalAPI.Services")
                            && (t.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase)
                                || t.Name.EndsWith("Services", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.NotEmpty(services);

            foreach (var service in services)
            {
                var ctors = service.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                foreach (var ctor in ctors)
                {
                    foreach (var param in ctor.GetParameters())
                    {
                        var paramType = param.ParameterType;
                        var isDbContext = typeof(DbContext).IsAssignableFrom(paramType)
                                          || paramType.Name.Equals("AppDbContext", StringComparison.OrdinalIgnoreCase);

                        Assert.False(
                            isDbContext,
                            $"El servicio de dominio {service.FullName} inyecta directamente {paramType.Name}. Toda persistencia debe pasar por IRepository."
                        );
                    }
                }
            }
        }

        [Fact]
        public void Fase3_TodosLosServiciosDeDominio_ImplementanInterfacesCorrespondientes()
        {
            var services = ApiAssembly.GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.Namespace != null
                            && t.Namespace.StartsWith("AdLocalAPI.Services")
                            && (t.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase)
                                || t.Name.EndsWith("Services", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.NotEmpty(services);

            foreach (var service in services)
            {
                var interfaces = service.GetInterfaces();
                var hasMatchingInterface = interfaces.Any(i =>
                    i.Name.StartsWith("I") &&
                    (i.Name.Contains("Service") || i.Name.Contains("Services")));

                Assert.True(
                    hasMatchingInterface,
                    $"El servicio de dominio {service.FullName} no implementa ninguna interfaz de servicio correspondiente (I*Service)."
                );
            }
        }

        [Fact]
        public void Fase3_TodosLosRepositorios_ImplementanInterfacesCorrespondientes()
        {
            var repositories = ApiAssembly.GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.Namespace != null
                            && t.Namespace.StartsWith("AdLocalAPI.Repositories")
                            && (t.Name.EndsWith("Repository", StringComparison.OrdinalIgnoreCase)
                                || t.Name.EndsWith("Repositorio", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.NotEmpty(repositories);

            foreach (var repo in repositories)
            {
                var interfaces = repo.GetInterfaces();
                var hasMatchingInterface = interfaces.Any(i =>
                    i.Name.StartsWith("I") &&
                    (i.Name.Contains("Repository") || i.Name.Contains("Repositorio")));

                Assert.True(
                    hasMatchingInterface,
                    $"El repositorio {repo.FullName} no implementa ninguna interfaz de repositorio correspondiente (I*Repository)."
                );
            }
        }

        // =========================================================================
        // 2. CONTRATOS API Y CONTROLADORES ESTANDARIZADOS (FASE 4)
        // =========================================================================

        [Fact]
        public void Fase4_TodosLosControladores_HeredanDeApiControllerBase()
        {
            var controllers = ApiAssembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(33, controllers.Count);

            foreach (var controller in controllers)
            {
                Assert.True(
                    typeof(ApiControllerBase).IsAssignableFrom(controller),
                    $"El controlador {controller.FullName} no hereda de ApiControllerBase."
                );
            }
        }

        [Fact]
        public void Fase4_TodasLasAccionesPublicas_DevuelvenIActionResult()
        {
            var controllers = ApiAssembly.GetTypes()
                .Where(t => typeof(ApiControllerBase).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var controller in controllers)
            {
                var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName && m.DeclaringType == controller);

                foreach (var action in actions)
                {
                    var returnType = action.ReturnType;
                    var isValid = typeof(IActionResult).IsAssignableFrom(returnType)
                                  || (returnType.IsGenericType
                                      && returnType.GetGenericTypeDefinition() == typeof(Task<>)
                                      && typeof(IActionResult).IsAssignableFrom(returnType.GetGenericArguments()[0]));

                    Assert.True(
                        isValid,
                        $"La acción {controller.Name}.{action.Name} devuelve {returnType.Name} en lugar de IActionResult o Task<IActionResult>."
                    );
                }
            }
        }

        [Fact]
        public void Fase4_ContratoApiResponse_SerializaStrictCamelCase()
        {
            var response = new ApiResponse<object>
            {
                Codigo = "200",
                Mensaje = "Operación exitosa",
                Respuesta = new { Id = 42, Nombre = "Prueba E2E" }
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };

            var json = JsonSerializer.Serialize(response, options);

            Assert.Contains("\"codigo\":\"200\"", json);
            Assert.Contains("\"mensaje\":\"Operación exitosa\"", json);
            Assert.Contains("\"respuesta\":", json);

            // No PascalCase en la raíz
            Assert.DoesNotContain("\"Codigo\":", json);
            Assert.DoesNotContain("\"Mensaje\":", json);
            Assert.DoesNotContain("\"Respuesta\":", json);

            // Deserialización exitosa
            var deserialized = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(json, options);
            Assert.NotNull(deserialized);
            Assert.Equal("200", deserialized.Codigo);
            Assert.Equal("Operación exitosa", deserialized.Mensaje);
            Assert.True(deserialized.EsExitoso);
        }

        [Theory]
        [InlineData("200", typeof(OkObjectResult), 200)]
        [InlineData("201", typeof(ObjectResult), 201)]
        [InlineData("400", typeof(BadRequestObjectResult), 400)]
        [InlineData("401", typeof(UnauthorizedObjectResult), 401)]
        [InlineData("403", typeof(ObjectResult), 403)]
        [InlineData("404", typeof(NotFoundObjectResult), 404)]
        [InlineData("409", typeof(ConflictObjectResult), 409)]
        [InlineData("500", typeof(ObjectResult), 500)]
        [InlineData("422", typeof(UnprocessableEntityObjectResult), 422)]
        public void Fase4_ApiControllerBase_Responder_MapeoExhaustivoCodigosHttp(
            string codigo, Type expectedResultType, int expectedStatusCode)
        {
            var controller = new TestController();
            var apiResponse = new ApiResponse<string>
            {
                Codigo = codigo,
                Mensaje = $"Mensaje para código {codigo}",
                Respuesta = "datos"
            };

            var actionResult = controller.ProbarResponder(apiResponse);

            Assert.IsType(expectedResultType, actionResult);
            if (actionResult is ObjectResult objResult)
            {
                Assert.Equal(expectedStatusCode, objResult.StatusCode);
                Assert.Same(apiResponse, objResult.Value);
            }
        }

        // =========================================================================
        // 3. MIDDLEWARE DE EXCEPCIONES Y BLINDAJE (FASE 4)
        // =========================================================================

        [Fact]
        public async Task Fase4_GlobalExceptionMiddleware_MapeaExcepcionesDominioYGenericas()
        {
            var mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

            // Caso 1: ReglaNegocioException -> 400
            await ProbarExcepcionAsync(
                new ReglaNegocioException("Monto inválido para comisión"),
                StatusCodes.Status400BadRequest,
                "400",
                "Monto inválido para comisión",
                mockLogger,
                mockEnv
            );

            // Caso 2: RecursoNoEncontradoException -> 404
            await ProbarExcepcionAsync(
                new RecursoNoEncontradoException("Comercio no existe"),
                StatusCodes.Status404NotFound,
                "404",
                "Comercio no existe",
                mockLogger,
                mockEnv
            );

            // Caso 3: ConflictoNegocioException -> 409
            await ProbarExcepcionAsync(
                new ConflictoNegocioException("El slug ya está en uso"),
                StatusCodes.Status409Conflict,
                "409",
                "El slug ya está en uso",
                mockLogger,
                mockEnv
            );

            // Caso 4: NoAutorizadoException -> 401
            await ProbarExcepcionAsync(
                new NoAutorizadoException("Token inválido"),
                StatusCodes.Status401Unauthorized,
                "401",
                "Token inválido",
                mockLogger,
                mockEnv
            );

            // Caso 5: Error Inesperado en Producción -> 500 Sanitizado con TraceIdentifier
            var contextProd = new DefaultHttpContext();
            contextProd.Response.Body = new MemoryStream();
            contextProd.TraceIdentifier = "trace-e2e-audit-999";

            RequestDelegate nextProd = _ => throw new Exception("Fallo inesperado y no controlado en producción");
            var middlewareProd = new GlobalExceptionMiddleware(nextProd, mockLogger.Object, mockEnv.Object);

            await middlewareProd.InvokeAsync(contextProd);

            Assert.Equal(StatusCodes.Status500InternalServerError, contextProd.Response.StatusCode);
            contextProd.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(contextProd.Response.Body);
            var json = await reader.ReadToEndAsync();

            Assert.Contains("trace-e2e-audit-999", json);
            Assert.DoesNotContain("Fallo inesperado y no controlado", json); // Jamás expone mensaje técnico en producción
        }

        private static async Task ProbarExcepcionAsync(
            Exception ex,
            int expectedStatus,
            string expectedCodigo,
            string expectedMensaje,
            Mock<ILogger<GlobalExceptionMiddleware>> logger,
            Mock<IWebHostEnvironment> env)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.TraceIdentifier = "trace-test-" + Guid.NewGuid();

            RequestDelegate next = _ => throw ex;
            var middleware = new GlobalExceptionMiddleware(next, logger.Object, env.Object);

            await middleware.InvokeAsync(context);

            Assert.Equal(expectedStatus, context.Response.StatusCode);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(expectedCodigo, result.Codigo);
            Assert.Equal(expectedMensaje, result.Mensaje);
        }

        // =========================================================================
        // 4. MÉTRICAS DE CÓDIGO: LÍMITE DE 500 LÍNEAS POR ARCHIVO EN SERVICES
        // =========================================================================

        [Fact]
        public void MetricasCodigo_NingunArchivoEnServices_Supera500Lineas()
        {
            var baseDir = AppContext.BaseDirectory;
            var solutionDir = Directory.GetParent(baseDir)?
                .Parent?.Parent?.Parent?.Parent?.FullName;

            if (string.IsNullOrEmpty(solutionDir) || !Directory.Exists(solutionDir))
            {
                solutionDir = @"c:\Users\USER\source\repos\AdLocalAPI";
            }

            var servicesDir = Path.Combine(solutionDir, "AdLocalAPI", "Services");
            if (!Directory.Exists(servicesDir))
            {
                // Fallback de búsqueda hacia arriba
                var current = new DirectoryInfo(baseDir);
                while (current != null && !Directory.Exists(Path.Combine(current.FullName, "AdLocalAPI", "Services")))
                {
                    current = current.Parent;
                }
                if (current != null)
                {
                    servicesDir = Path.Combine(current.FullName, "AdLocalAPI", "Services");
                }
            }

            Assert.True(Directory.Exists(servicesDir), $"No se encontró el directorio Services en {servicesDir}");

            var files = Directory.GetFiles(servicesDir, "*.cs", SearchOption.AllDirectories);
            Assert.NotEmpty(files);

            var filesExceedingLimit = new List<string>();

            foreach (var file in files)
            {
                var lineCount = File.ReadAllLines(file).Length;
                if (lineCount > 500)
                {
                    var relativePath = Path.GetRelativePath(servicesDir, file);
                    filesExceedingLimit.Add($"{relativePath} ({lineCount} líneas)");
                }
            }

            Assert.Empty(filesExceedingLimit);
        }
    }
}
