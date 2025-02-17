using EMR.Application.Interfaces.Services;
using EMR.Application.Requests;

namespace EMR.API.Controllers.v1.Otp;

public class OtpController : BaseApiController<OtpController>
{
    private readonly IOtpService _otpService;

    public OtpController(IOtpService otpService)
    {
        _otpService = otpService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestEmailOtpAsync(EmailOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _otpService.RequestEmailOtpAsync(request, cancellationToken);
        if (result.Succeeded) return Ok(result);

        return NotFound(result);
    }

    [HttpPost("resend")]
    public async Task<IActionResult> ResendEmailOtpAsync(ResendEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _otpService.ResendEmailOtpAsync(request, cancellationToken);
        if (result.Succeeded) return Ok(result);

        return NotFound(result);
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateEmailOtpAsync(ValidateEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _otpService.ValidateEmailOtpAsync(request, cancellationToken);
        if (result.Succeeded) return Ok(result);

        return NotFound(result);
    }
}