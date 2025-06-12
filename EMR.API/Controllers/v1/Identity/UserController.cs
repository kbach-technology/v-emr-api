using EMR.Application.Interfaces.Services.Identity;
using EMR.Application.Requests.Identity;
using EMR.Application.Requests.Keycloaks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace EMR.API.Controllers.v1.Identity;

public class UserController : BaseApiController<UserController>
{
    private readonly IKeycloakService _keycloakService;
    private readonly IIdentityService _identityService;

    public UserController(IKeycloakService keycloakService, IIdentityService identityService)
    {
        _keycloakService = keycloakService;
        _identityService = identityService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.CreateUserAsync(request, cancellationToken);
        if (result.Succeeded)
            return Ok(await _identityService.GetUserAsync(result.Data, cancellationToken));
        return BadRequest(result);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> AmendUserAsync(AmendUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.AmendUserAsync(request, cancellationToken);
        if (result.Succeeded)
            return Ok(await _identityService.GetUserAsync(request.Id, cancellationToken));
        return BadRequest(result);
    }

    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> ToggleUserAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _identityService.ToggleUserAsync(id, cancellationToken);
        if (result.Succeeded)
            return Ok(await _identityService.GetUserAsync(id, cancellationToken));
        return BadRequest(result);
    }

    [HttpPost("password/validate")]
    public async Task<IActionResult> ValidateCurrentPasswordAsync(ValidateCurrentPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _keycloakService.ValidateCurrentPassword(request.CurrentPassword, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpPut("password/change")]
    public async Task<IActionResult> ChangePasswordAsync(ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.ChangePasswordAsync(request, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpPut("password/self-reset")]
    public async Task<IActionResult> SelfResetPasswordAsync(ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.SelfResetPasswordAsync(request, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpPut("password/admin-reset")]
    public async Task<IActionResult> AdminResetUserPasswordAsync(ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.AdminResetUserPasswordAsync(request, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsersAsync(int pageNumber, int pageSize, bool? isActive, string? searchString,
        CancellationToken cancellationToken)
    {
        var result =
            await _identityService.GetUsersAsync(pageNumber, pageSize, isActive, searchString, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _identityService.GetUserAsync(id, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}/sessions")]
    public async Task<IActionResult> GetUserSessionsAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _keycloakService.GetUserSessionAsync(id, cancellationToken);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }
}