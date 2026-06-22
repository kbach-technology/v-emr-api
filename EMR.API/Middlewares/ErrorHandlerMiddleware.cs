using System.Net;
using EMR.Application.Exceptions;
using EMR.Shared.Wrapper;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EMR.API.Middlewares;

public class ErrorHandlerMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            var responseModel = await Result<string>.FailAsync(error.Message);

            switch (error)
            {
                case ApiException e:
                    // custom application error
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case KeyNotFoundException e:
                    // not found error
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case UnauthorizedAccessException e:
                    // unauthorized access
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    responseModel = await Result<string>.FailAsync("Unauthorized access");
                    break;

                case SecurityTokenException e:
                    // JWT token errors (invalid, expired, etc.)
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    responseModel = await Result<string>.FailAsync("Invalid or expired token");
                    break;

                default:
                    // unhandled error
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    // Don't expose internal error details in production
                    if (!environment.IsDevelopment())
                    {
                        responseModel = await Result<string>.FailAsync("An internal error occurred");
                    }
                    break;
            }

            var result = JsonConvert.SerializeObject(
                responseModel,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
            );
            await response.WriteAsync(result);
        }
    }
}