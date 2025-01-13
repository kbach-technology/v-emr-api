using MediatR;

namespace EMR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController<T> : ControllerBase
{
    private ILogger<T> _loggerInstance;
    private IMediator _mediatorInstance;
    protected IMediator _mediator => _mediatorInstance ??= HttpContext.RequestServices.GetService<IMediator>();
    protected ILogger<T> _logger => _loggerInstance ??= HttpContext.RequestServices.GetService<ILogger<T>>();
}