using EMR.Application.Configurations;
using FluentValidation.AspNetCore;

namespace EMR.API.Extentions;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddValidators(this IMvcBuilder builder)
    {
        builder.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<AppConfiguration>());
        return builder;
    }

    public static void AddExtendedAttributesValidators(this IServiceCollection services)
    {
    }
}