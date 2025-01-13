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

        RuleFor(x => x.IdentifierType)
            .IsInEnum().WithMessage(_localizer["Invalid identifier type."]);

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage(_localizer["Identifier is required."])
            .DependentRules(() =>
            {
                When(x => x.IdentifierType == IdentifierType.PhoneNumber, () =>
                {
                    RuleFor(x => x.Identifier)
                        .Matches(@"^(\+855|0)[1-9][0-9]{7,8}$")
                        .WithMessage(_localizer["Invalid Cambodian phone number."]);
                });

                When(x => x.IdentifierType == IdentifierType.Email, () =>
                {
                    RuleFor(x => x.Identifier)
                        .EmailAddress()
                        .WithMessage(_localizer["Invalid email address."]);
                });

                When(x => x.IdentifierType == IdentifierType.Username, () =>
                {
                    RuleFor(x => x.Identifier)
                        .MinimumLength(3)
                        .MaximumLength(50)
                        .Matches(@"^[a-zA-Z0-9_-]+$")
                        .WithMessage(_localizer[
                            "Username must be 3-50 characters long and can only contain letters, numbers, underscores, and hyphens."]);
                });
            });

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage(_localizer["Pin is required."])
            .DependentRules(() =>
            {
                When(x => x.IdentifierType == IdentifierType.Email, () =>
                    {
                        RuleFor(x => x.Pin)
                            .MinimumLength(6).WithMessage(_localizer["Password must be at least 6 characters long."])
                            .MaximumLength(20).WithMessage(_localizer["Password cannot exceed 20 characters."])
                            .Matches(@"[A-Z]")
                            .WithMessage(_localizer["Password must contain at least one uppercase letter."])
                            .Matches(@"[a-z]")
                            .WithMessage(_localizer["Password must contain at least one lowercase letter."])
                            .Matches(@"[0-9]").WithMessage(_localizer["Password must contain at least one number."])
                            .Matches(@"[!@#$%^&*(),.?""':{}|<>]")
                            .WithMessage(_localizer["Password must contain at least one special character."]);
                    })
                    .Otherwise(() =>
                    {
                        RuleFor(x => x.Pin)
                            .Length(4).WithMessage(_localizer["Pin must be exactly 4 characters long."])
                            .Matches("^[0-9]{4}$").WithMessage(_localizer["Pin must consist of numeric digits only."]);
                    });
            });
    }
}