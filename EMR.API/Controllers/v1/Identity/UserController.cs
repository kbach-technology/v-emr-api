using EMR.Application.Interfaces.Services.Keycloak;
using EMR.Application.Requests.Keycloaks;

namespace EMR.API.Controllers.v1.Identity;

public class UserController : BaseApiController<UserController>
{
    private readonly IKeycloakService _keycloakService;

    public UserController(IKeycloakService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUserAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _keycloakService.LoginAsync(request, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _keycloakService.RefreshTokenAsync(request, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }
}