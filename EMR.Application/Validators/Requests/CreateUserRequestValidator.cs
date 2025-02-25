using System.Data;
using EMR.Application.Requests.Keycloaks;
using FluentValidation;
using EMR.Domain.Enums;

namespace EMR.Application.Validators.Requests;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private readonly IStringLocalizer<CreateUserRequestValidator> _localizer;

    public CreateUserRequestValidator(IStringLocalizer<CreateUserRequestValidator> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_localizer["Email is required."])
            .EmailAddress().WithMessage(_localizer["Email is not valid."]);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(_localizer["Full name is required."])
            .MinimumLength(6).WithMessage(_localizer[_localizer["Full name must be at least 6 characters long."]])
            .MaximumLength(255).WithMessage(_localizer["Full name cannot exceed 255 characters."]);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(_localizer["Phone number is required."]);
    }
}