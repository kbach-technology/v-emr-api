using EMR.Application.Requests;
using FluentValidation;

namespace EMR.Application.Validators.Requests;

public class OtpRequestValidator : AbstractValidator<OtpRequest>
{
    public OtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty();
    }
}