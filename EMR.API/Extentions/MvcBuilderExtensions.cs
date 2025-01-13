using FluentValidation.AspNetCore;
using EMR.Application.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace GOJOR.API.Extensions;

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