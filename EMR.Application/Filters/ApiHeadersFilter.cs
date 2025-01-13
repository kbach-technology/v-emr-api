using EMR.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EMR.Application.Filters;

public class ApiHeadersFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var headers = context.HttpContext.Request.Headers;

        if (!headers.ContainsKey("X-Api-Version") ||
            !headers.ContainsKey("User-Agent") ||
            !headers.ContainsKey("Accept-Language") ||
            !headers.ContainsKey("Platform"))
        {
            context.Result = new BadRequestObjectResult("Missing required headers.");
            return;
        }

        var apiHeaders = new ApiHeaders
        {
            ApiVersion = headers["X-Api-Version"].ToString(),
            UserAgent = headers["User-Agent"].ToString(),
            LanguageCode = headers["Accept-Language"].ToString(),
            Platform = headers["Platform"].ToString()
        };

        context.HttpContext.Items["ApiHeaders"] = apiHeaders;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}