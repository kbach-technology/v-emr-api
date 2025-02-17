using EMR.API.Extentions;
using EMR.API.Middlewares;
using EMR.API.SecurityHeader;
using EMR.Application.Extensions;
using EMR.Application.Filters;
using EMR.Shared.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;


builder.Host.UseSerilog();
builder.Host.ConfigureAppConfiguration();    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddForwarding(configuration);
builder.Services.AddSerialization();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddServerLocalization();
builder.Services.AddSingleton(Log.Logger);
builder.Services.AddCurrentUserService();
builder.Services.AddDatabase(configuration);
//builder.Services.AddPermissionServices();
builder.Services.AddKeyloakConfiguration(configuration);
builder.Services.AddIdentityServer(configuration);
builder.Services.AddSignalR();
builder.Services.AddApplicationLayer();
builder.Services.AddApplicationServices();
builder.Services.AddRateLimitServices(configuration);
builder.Services.AddRepositories();
builder.Services.AddSharedInfrastructure(configuration);
builder.Services.AddInfrastructureMappings();
builder.Services.RegisterSwagger();

builder.Services.AddControllers(option => option.Filters.Add<ApiHeadersFilter>())
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver =
            new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    })
    .AddValidators();

builder.Services.AddCorsPolicy();
builder.Services.AddExtendedAttributesValidators();
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ReportApiVersions = true;
});

builder.Services.AddApiVersion();
builder.Services.AddHttpClient();
builder.Services.AddLazyCache();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var errors = actionContext.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .Select(e => new Problems
            {
                Title = "One or more validation errors occurred.",
                Message = e.Value.Errors.First().ErrorMessage,
                Succeeded = false
            }).FirstOrDefault();

        return new BadRequestObjectResult(errors);
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseSecurityHeaders(configuration);
app.UseHttpsRedirection();

app.UseRequestLocalizationByCulture();
app.Initialize(configuration, CancellationToken.None);
app.UseRouting();

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseEndpoints(endpoints => { endpoints.MapControllers(); });


app.Run();