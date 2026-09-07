using AdLocalAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult Responder<T>(ApiResponse<T>? response)
        {
            if (response == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<T>.Error("500", "Respuesta nula del servicio")
                );
            }

            return response.Codigo switch
            {
                "200" => Ok(response),
                "201" => StatusCode(StatusCodes.Status201Created, response),
                "204" => NoContent(),
                "400" => BadRequest(response),
                "401" => Unauthorized(response),
                "403" => StatusCode(StatusCodes.Status403Forbidden, response),
                "404" => NotFound(response),
                "409" => Conflict(response),
                "422" => UnprocessableEntity(response),
                "500" => StatusCode(StatusCodes.Status500InternalServerError, response),
                _ when int.TryParse(response.Codigo, out int status) && status >= 100 && status <= 599
                    => StatusCode(status, response),
                _ => BadRequest(response)
            };
        }

        protected IActionResult Responder(ApiResponse? response) => Responder<object>(response);

        protected IActionResult ResponderExito<T>(T data, string mensaje = "Operación exitosa")
            => Ok(ApiResponse<T>.Success(data, mensaje));

        protected IActionResult ResponderError(string codigo, string mensaje)
            => Responder(ApiResponse.Error(codigo, mensaje));

        protected IActionResult ResponderError(string mensaje, int statusCode = 400)
            => Responder(ApiResponse.Error(statusCode.ToString(), mensaje));
    }
}
