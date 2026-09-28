using System;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdLocalAPI.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var error = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => !string.IsNullOrEmpty(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Datos de entrada inválidos.";

                context.Result = new BadRequestObjectResult(ApiResponse.Error("400", error));
                return;
            }

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var argType = argument.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(argType);

                if (context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator)
                {
                    var validationContext = new ValidationContext<object>(argument);
                    var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                    if (!validationResult.IsValid)
                    {
                        var firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Error de validación en la solicitud.";
                        context.Result = new BadRequestObjectResult(ApiResponse.Error("400", firstError));
                        return;
                    }
                }
            }

            await next();
        }
    }
}
