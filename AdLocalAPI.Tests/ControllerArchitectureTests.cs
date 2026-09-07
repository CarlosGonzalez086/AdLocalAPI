using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AdLocalAPI.Controllers;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class ControllerArchitectureTests
    {
        private static readonly Assembly ApiAssembly = typeof(ApiControllerBase).Assembly;

        private static List<Type> GetAllControllers()
        {
            return ApiAssembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        [Fact]
        public void TodosLosControladores_DebenHeredarDeApiControllerBase()
        {
            var controllers = GetAllControllers();

            Assert.NotEmpty(controllers);

            foreach (var controller in controllers)
            {
                Assert.True(
                    typeof(ApiControllerBase).IsAssignableFrom(controller),
                    $"El controlador {controller.FullName} no hereda de ApiControllerBase."
                );
            }
        }

        [Fact]
        public void NingunControlador_DebeInyectarAppDbContextDirectamente()
        {
            var controllers = GetAllControllers();

            foreach (var controller in controllers)
            {
                var constructors = controller.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                foreach (var ctor in constructors)
                {
                    foreach (var param in ctor.GetParameters())
                    {
                        Assert.False(
                            typeof(DbContext).IsAssignableFrom(param.ParameterType),
                            $"El controlador {controller.FullName} inyecta directamente un DbContext ({param.ParameterType.Name}). Debe pasar por un IService."
                        );
                    }
                }
            }
        }

        [Fact]
        public void NingunControlador_DebeInyectarRepositoriosDirectamente()
        {
            var controllers = GetAllControllers();

            foreach (var controller in controllers)
            {
                var constructors = controller.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                foreach (var ctor in constructors)
                {
                    foreach (var param in ctor.GetParameters())
                    {
                        var paramTypeName = param.ParameterType.Name;
                        var isRepo = paramTypeName.EndsWith("Repository", StringComparison.OrdinalIgnoreCase)
                                     || paramTypeName.EndsWith("Repositorio", StringComparison.OrdinalIgnoreCase);

                        Assert.False(
                            isRepo,
                            $"El controlador {controller.FullName} inyecta directamente el repositorio {paramTypeName}. Debe pasar por un IService."
                        );
                    }
                }
            }
        }

        [Fact]
        public void TodasLasAccionesPublicas_DebenDevolverIActionResult()
        {
            var controllers = GetAllControllers();

            foreach (var controller in controllers)
            {
                var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName);

                foreach (var method in methods)
                {
                    var returnType = method.ReturnType;
                    var isValidReturn = typeof(IActionResult).IsAssignableFrom(returnType)
                                        || (returnType.IsGenericType
                                            && returnType.GetGenericTypeDefinition() == typeof(Task<>)
                                            && typeof(IActionResult).IsAssignableFrom(returnType.GetGenericArguments()[0]));

                    Assert.True(
                        isValidReturn,
                        $"El método {controller.Name}.{method.Name} devuelve {returnType.Name} en lugar de IActionResult o Task<IActionResult>."
                    );
                }
            }
        }

        [Fact]
        public async Task ConfiguracionController_ProbarCorreo_DelegaEnIConfiguracionService()
        {
            var mockService = new Mock<IConfiguracionService>();
            mockService.Setup(s => s.ProbarCorreoAsync("test@adlocal.store"))
                .ReturnsAsync(ApiResponse<object>.Success(null, "Correo enviado"));

            var controller = new ConfiguracionController(mockService.Object);
            var result = await controller.ProbarCorreo(new EmailDto { Email = "test@adlocal.store" });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsAssignableFrom<ApiResponse<object>>(okResult.Value);
            Assert.Equal("200", response.Codigo);
            Assert.Equal("Correo enviado", response.Mensaje);

            mockService.Verify(s => s.ProbarCorreoAsync("test@adlocal.store"), Times.Once);
        }
    }
}
