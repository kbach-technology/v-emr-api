using EMR.Application.Requests.Keycloaks;
using FluentValidation;
using EMR.Domain.Enums;

namespace EMR.Application.Validators.Requests;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    private readonly IStringLocalizer<LoginRequestValidator> _localizer;

    public LoginRequestValidator(IStringLocalizer<LoginRequestValidator> localizer)
    {
        _localizer = localizer;
        
        // RuleFor(x => x.Email)
        //     .NotEmpty().WithMessage(_localizer["Email is required."])
        //     .EmailAddress().WithMessage(_localizer["Email is not valid."]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_localizer["Pin is required."]);
        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage(_localizer["Password must be at least 6 characters long."])
            .MaximumLength(20).WithMessage(_localizer["Password cannot exceed 20 characters."])
            .Matches(@"[A-Z]")
            .WithMessage(_localizer["Password must contain at least one uppercase letter."])
            .Matches(@"[a-z]")
            .WithMessage(_localizer["Password must contain at least one lowercase letter."])
            .Matches(@"[0-9]").WithMessage(_localizer["Password must contain at least one number."])
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]")
            .WithMessage(_localizer["Password must contain at least one special character."]);
    }
}