using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HotelPlatform.ServiceDefaults.Swagger;

public static class PlatformSwaggerExtensions
{
    public static SwaggerGenOptions AddPlatformOAuth(
        this SwaggerGenOptions options,
        IConfiguration configuration,
        params string[] scopes)
    {
        var authority = configuration["IdentityServer:PublicAuthority"]?.TrimEnd('/')
            ?? "http://localhost:15135";

        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority}/connect/authorize"),
                    TokenUrl = new Uri($"{authority}/connect/token"),
                    Scopes = scopes.ToDictionary(scope => scope, scope => $"Hotel platform {scope} access")
                }
            }
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            }] = scopes
        });

        return options;
    }
}
