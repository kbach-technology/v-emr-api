using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EMR.Application.Filters;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values.SelectMany(v => v.Errors);
            var errorMessage = string.Join(" and ", errors.Select(e => e.ErrorMessage));
            context.Result = new OkObjectResult(
                new Result<string> { Succeeded = false, Message = errorMessage });
        }
    }
}