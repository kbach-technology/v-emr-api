using EMR.Application.Interfaces.Serialization.Serializers;
using FluentValidation;
using FluentValidation.Validators;

namespace EMR.Application.Validators;

public class JsonValidator<T>(IJsonSerializer jsonSerializer) : PropertyValidator<T, string>
{
    public override string Name => "JsonValidator";

    public override bool IsValid(ValidationContext<T> context, string value)
    {
        var isJson = true;
        value = value.Trim();
        try
        {
            jsonSerializer.Deserialize<object>(value);
        }
        catch
        {
            isJson = false;
        }

        isJson = (isJson && value.StartsWith("{") && value.EndsWith("}"))
                 || (value.StartsWith("[") && value.EndsWith("]"));

        return isJson;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "{PropertyName} must be json string.";
    }
}