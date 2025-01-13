using System.Globalization;
using EMR.API.Middlewares;
using EMR.Shared.Constants.Localization;
using Microsoft.AspNetCore.Localization;

namespace EMR.API.Extentions;

internal static class ApplicationBuilderExtensions
{
    internal static IApplicationBuilder UseExceptionHandling(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        return app;
    }

    internal static void ConfigureSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", typeof(Program).Assembly.GetName().Name);
            options.RoutePrefix = "swagger";
            options.DisplayRequestDuration();
        });
    }

    // internal static IApplicationBuilder UseEndpoints(this IApplicationBuilder app)
    // {
    //     return app.UseEndpoints(endpoints =>
    //     {
    //         endpoints.MapControllers();
    //         endpoints.MapHub<SignalRHub>(ApplicationConstants.SignalR.HubUrl);
    //     });
    // }

    internal static IApplicationBuilder UseRequestLocalizationByCulture(this IApplicationBuilder app)
    {
        var supportedCultures = LocalizationConstants.SupportedLanguages.Select(l => new CultureInfo(l.Code)).ToArray();
        app.UseRequestLocalization(options =>
        {
            options.SupportedUICultures = supportedCultures;
            options.SupportedCultures = supportedCultures;
            options.DefaultRequestCulture = new RequestCulture(supportedCultures.First());
            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        app.UseMiddleware<RequestCultureMiddleware>();

        return app;
    }
}