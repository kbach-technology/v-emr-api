using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using AspNetCoreRateLimit;
using EMR.API.Localization;
using EMR.Application.Attributes;
using EMR.Application.Configurations;
using EMR.Application.Interfaces.Services;
using EMR.Application.Interfaces.Services.Keycloak;
using EMR.Application.Services;
using EMR.Application.Services.Keycloak;
using EMR.BlogStorage;
using EMR.Persistence.Contexts;
using EMR.Redis;
using EMR.Shared.Interfaces;
using EMR.Shared.Models;
using EMR.Shared.Services;
using EMR.SMS.Services;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace EMR.API.Extentions;

internal static class ServiceCollectionExtensions
{
    public static AppConfiguration GetApplicationSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var applicationSettingsConfiguration = configuration.GetSection(nameof(AppConfiguration));
        services.Configure<AppConfiguration>(applicationSettingsConfiguration);
        return applicationSettingsConfiguration.Get<AppConfiguration>();
    }

    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddDbContext<AppDbContext>(options => options
                .UseNpgsql(configuration.GetConnectionString("GOJORConnection"))
                .EnableSensitiveDataLogging());
    }

    internal static IServiceCollection AddServerLocalization(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(ServerLocalizer<>));
        return services;
    }

    public static IServiceCollection AddCurrentUserService(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<ICurrentUserService, CurrentUserService>();
        return services;
    }

    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBlobStorage(configuration);
        services.AddRedisCacheStorage(configuration);
        services.AddTransient<IDateTimeService, SystemDateTimeService>();
        services.Configure<MailConfiguration>(configuration.GetSection("MailConfiguration"));
        return services;
    }

    internal static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<IKeycloakService, KeycloakService>();
        services.AddTransient<INumService, NumService>();
        services.AddTransient<IAppVersionService, AppVersionService>();
        services.AddTransient<IOtpService, OtpService>();
        services.AddTransient<ISmsService, SmsService>();

        return services;
    }

    public static IServiceCollection AddRateLimitServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services
            .AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddMemoryCache();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    internal static void RegisterSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(async c =>
        {
            //TODO - Lowercase Swagger Documents
            //c.DocumentFilter<LowercaseDocumentFilter>();
            //Refer - https://gist.github.com/rafalkasa/01d5e3b265e5aa075678e0adfd54e23f

            // include all project's xml comments
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (!assembly.IsDynamic)
                {
                    var xmlFile = $"{assembly.GetName().Name}.xml";
                    var xmlPath = Path.Combine(baseDirectory, xmlFile);
                    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
                }

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "GOJOR.AdminAPI",
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description =
                    "Input your Bearer token in this format - Bearer {your token here} to access this GOJOR.GOJOR.Communication"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "Bearer",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });
        });
    }

    internal static IServiceCollection AddIdentityServer(this IServiceCollection services,
        IConfiguration config)
    {
        var key = Encoding.UTF8.GetBytes(config["Secret"]);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(bearer =>
        {
            bearer.Authority = $"{config["Keycloak:Client:Authority"]}/realms/{config["Keycloak:Client:Realm"]}";
            bearer.RequireHttpsMetadata = config.GetValue<bool>("Keycloak:Client:RequireHttpsMetadata");
            bearer.SaveToken = true;
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer =
                    $"{config["Keycloak:Client:Authority"]}/realms/{config["Keycloak:Client:Realm"]}", // Ensure this matches the issuer in the token
                ValidateAudience = false,
                ValidAudience = config["Keycloak:Client:ClientId"],
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.Zero
            };

            bearer.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) &&
                        context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var token = context.SecurityToken as JwtSecurityToken;
                    if (token != null)
                    {
                        var introspectionClient = new HttpClient();
                        var introspectionRequest = new TokenIntrospectionRequest
                        {
                            Address =
                                $"{config["Keycloak:Client:Authority"]}/realms/{config["Keycloak:Client:Realm"]}/protocol/openid-connect/token/introspect",
                            ClientId = config["Keycloak:Client:ClientId"],
                            ClientSecret = config["Keycloak:Client:Secret"],
                            Token = token.RawData
                        };

                        var response = await introspectionClient.IntrospectTokenAsync(introspectionRequest);
                        if (!response.IsActive)
                            context.Fail(JsonConvert.SerializeObject(new
                                { message = "Token is not active", succeeded = false }));
                    }
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = "application/json";
                        var authFailedResponse = JsonConvert.SerializeObject(new
                            { message = "The Token is expired.", succeeded = false });
                        return context.Response.WriteAsync(authFailedResponse);
                    }
#if DEBUG
                    context.NoResult();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "text/plain";
                    return context.Response.WriteAsync(context.Exception.ToString());
#else
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.ContentType = "application/json";
                        var authFailedResp = JsonConvert.SerializeObject(new { message =
 "An unhandled error has occurred.", succeeded = false });
                        return context.Response.WriteAsync(authFailedResp);
#endif
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = "application/json";
                        var challengeResponse =
                            JsonConvert.SerializeObject(new { message = "You are not Authorized.", succeeded = false });
                        return context.Response.WriteAsync(challengeResponse);
                    }

                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    context.Response.ContentType = "application/json";
                    var forbiddenResponse = JsonConvert.SerializeObject(new Problems
                    {
                        Message = "You are not authorized to access this resource.",
                        Succeeded = false
                    });
                    return context.Response.WriteAsync(forbiddenResponse);
                }
            };
        });


        services.AddAuthorization(options =>
        {
            // Dynamically add policies based on Permissions class
            foreach (var prop in typeof(Permissions).GetNestedTypes().SelectMany(c =>
                         c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
            {
                var propertyValue = prop.GetValue(null)?.ToString();
                if (!string.IsNullOrEmpty(propertyValue))
                    options.AddPolicy(propertyValue, policy =>
                        policy.Requirements.Add(new PermissionRequirement(new[] { propertyValue })));
            }
        });

        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

        return services;
    }
}