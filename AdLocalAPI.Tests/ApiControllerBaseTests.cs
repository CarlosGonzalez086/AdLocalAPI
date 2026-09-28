using System;
using AdLocalAPI.Controllers;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class ApiControllerBaseTests
    {
        private class TestApiController : ApiControllerBase
        {
            public IActionResult TestResponder<T>(ApiResponse<T>? response) => Responder(response);
            public IActionResult TestResponderNonGeneric(ApiResponse? response) => Responder(response);
            public IActionResult TestResponderExito<T>(T data, string mensaje = "Operación exitosa") => ResponderExito(data, mensaje);
            public IActionResult TestResponderError(string mensaje, int statusCode = 400) => ResponderError(mensaje, statusCode);
        }

        private readonly TestApiController _controller = new();

        [Fact]
        public void Responder_NullResponse_Returns500InternalServerError()
        {
            var result = _controller.TestResponder<string>(null);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

            var apiResponse = Assert.IsAssignableFrom<ApiResponse<string>>(objectResult.Value);
            Assert.Equal("500", apiResponse.Codigo);
            Assert.False(apiResponse.EsExitoso);
        }

        [Fact]
        public void Responder_Code200_ReturnsOkObjectResult()
        {
            var response = ApiResponse<string>.Success("test data", "OK");
            var result = _controller.TestResponder(response);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Same(response, okResult.Value);
            Assert.True(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code201_ReturnsCreatedObjectResult()
        {
            var response = new ApiResponse<string> { Codigo = "201", Mensaje = "Created", Respuesta = "new item" };
            var result = _controller.TestResponder(response);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            Assert.Same(response, objectResult.Value);
            Assert.True(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code204_ReturnsNoContentResult()
        {
            var response = new ApiResponse<string> { Codigo = "204", Mensaje = "No content", Respuesta = null };
            var result = _controller.TestResponder(response);

            Assert.IsType<NoContentResult>(result);
            Assert.True(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code400_ReturnsBadRequestObjectResult()
        {
            var response = ApiResponse<string>.BadRequest("Petición inválida");
            var result = _controller.TestResponder(response);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Same(response, badRequestResult.Value);
            Assert.False(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code401_ReturnsUnauthorizedObjectResult()
        {
            var response = ApiResponse<string>.Unauthorized("No autorizado");
            var result = _controller.TestResponder(response);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            Assert.Same(response, unauthorizedResult.Value);
            Assert.False(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code403_Returns403ObjectResultWithBody()
        {
            var response = ApiResponse<string>.Forbid("Acceso prohibido");
            var result = _controller.TestResponder(response);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
            Assert.Same(response, objectResult.Value);
            Assert.False(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code404_ReturnsNotFoundObjectResult()
        {
            var response = ApiResponse<string>.NotFound("Elemento no encontrado");
            var result = _controller.TestResponder(response);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            Assert.Same(response, notFoundResult.Value);
            Assert.False(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code409_ReturnsConflictObjectResult()
        {
            var response = ApiResponse<string>.Conflict("Conflicto de recursos");
            var result = _controller.TestResponder(response);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
            Assert.Same(response, conflictResult.Value);
            Assert.False(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code422_ReturnsUnprocessableEntityObjectResult()
        {
            var response = new ApiResponse<string> { Codigo = "422", Mensaje = "Entidad no procesable", Respuesta = null };
            var result = _controller.TestResponder(response);

            var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessableResult.StatusCode);
            Assert.Same(response, unprocessableResult.Value);
            Assert.False(response.EsExitoso);
        }

        [Fact]
        public void Responder_Code500_Returns500ObjectResult()
        {
            var response = ApiResponse<string>.InternalServerError("Error interno del servidor");
            var result = _controller.TestResponder(response);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Same(response, objectResult.Value);
            Assert.False(response.EsExitoso);
        }

        [Fact]
        public void Responder_CustomNumericCode_ReturnsObjectResultWithCorrespondingStatus()
        {
            var response = new ApiResponse<string> { Codigo = "418", Mensaje = "Soy una tetera", Respuesta = "Teapot" };
            var result = _controller.TestResponder(response);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(418, objectResult.StatusCode);
            Assert.Same(response, objectResult.Value);
        }

        [Fact]
        public void Responder_NonNumericCode_DefaultsTo400BadRequest()
        {
            var response = new ApiResponse<string> { Codigo = "CUSTOM_ERR", Mensaje = "Error no numérico", Respuesta = null };
            var result = _controller.TestResponder(response);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Same(response, badRequestResult.Value);
        }

        [Fact]
        public void ResponderNonGeneric_ReturnsCorrectResult()
        {
            var response = ApiResponse.Success("Éxito no genérico");
            var result = _controller.TestResponderNonGeneric(response);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Same(response, okResult.Value);
        }

        [Fact]
        public void ResponderExito_Returns200WithPayload()
        {
            var result = _controller.TestResponderExito(12345, "Operación exitosa");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var response = Assert.IsType<ApiResponse<int>>(okResult.Value);
            Assert.Equal("200", response.Codigo);
            Assert.Equal("Operación exitosa", response.Mensaje);
            Assert.Equal(12345, response.Respuesta);
            Assert.True(response.EsExitoso);
        }

        [Fact]
        public void ResponderError_ReturnsSpecifiedStatusCodeWithPayload()
        {
            var result = _controller.TestResponderError("Acceso no permitido", StatusCodes.Status403Forbidden);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

            var response = Assert.IsAssignableFrom<ApiResponse<object>>(objectResult.Value);
            Assert.Equal("403", response.Codigo);
            Assert.Equal("Acceso no permitido", response.Mensaje);
            Assert.False(response.EsExitoso);
        }

        [Theory]
        [InlineData("200", true)]
        [InlineData("201", true)]
        [InlineData("204", true)]
        [InlineData("400", false)]
        [InlineData("401", false)]
        [InlineData("403", false)]
        [InlineData("404", false)]
        [InlineData("409", false)]
        [InlineData("500", false)]
        [InlineData("invalid", false)]
        [InlineData(null, false)]
        public void EsExitoso_EvaluatesCorrectly(string? codigo, bool expectedSuccess)
        {
            var response = new ApiResponse<string> { Codigo = codigo!, Mensaje = "msg" };
            Assert.Equal(expectedSuccess, response.EsExitoso);
        }
    }
}
