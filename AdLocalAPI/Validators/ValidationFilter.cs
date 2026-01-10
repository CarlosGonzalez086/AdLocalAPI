//using AdLocalAPI.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Filters;

//namespace AdLocalAPI.Validators
//{
//    public class ValidationFilter : IAsyncActionFilter
//    {

//        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//        {
//            if (!context.ModelState.IsValid)
//            {
//                var errors = context.ModelState
//                    .Where(x => x.Value?.Errors.Count > 0)
//                    .ToDictionary(
//                        kvp => kvp.Key,
//                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
//                    );

//                context.Result = new BadRequestObjectResult(ApiResponse.Error("400", "Validación fallida", errors));
//                return;
//            }

//            await next();
//        }
//    }
//}
