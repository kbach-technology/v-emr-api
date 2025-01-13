using EMR.Application.Requests.Keycloaks;
using FluentValidation;
using EMR.Domain.Enums;

namespace EMR.Application.Validators.Requests;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private readonly IStringLocalizer<RegisterRequestValidator> _localizer;

    public RegisterRequestValidator(IStringLocalizer<RegisterRequestValidator> localizer)
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
            .Length(4).WithMessage(_localizer["Pin must be exactly 4 characters long."])
            .Matches("^[0-9]{4}$").WithMessage(_localizer["Pin must consist of numeric digits only."]);
    }
}