using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace HotelPlatform.ServiceDefaults.Authentication;

public static class PlatformAuthenticationExtensions
{
    public const string PlatformBearerScheme = "PlatformBearer";
    private const string HotelAuthScheme = "HotelAuth";
    private const string IdentityServerScheme = "IdentityServer";
    private const string KeycloakScheme = "Keycloak";

    public static AuthenticationBuilder AddPlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string identityServerAudience,
        string keycloakAudience)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return services.AddAuthentication();
        }

        var authIssuer = configuration["Jwt:Issuer"] ?? "HotelAuth.Api";
        var authAudience = configuration["Jwt:Audience"] ?? "hotel-platform";
        var signingKey = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey must be supplied as a secret and contain at least 32 bytes.");
        }

        var identityServerAuthority = configuration["IdentityServer:Authority"];
        if (string.IsNullOrWhiteSpace(identityServerAuthority))
        {
            throw new InvalidOperationException("IdentityServer:Authority must be configured for protected services.");
        }

        var keycloakAuthority = configuration["Keycloak:Authority"];
        if (!string.IsNullOrWhiteSpace(keycloakAuthority)
            && !keycloakAuthority.Contains("/realms/", StringComparison.OrdinalIgnoreCase))
        {
            keycloakAuthority = $"{keycloakAuthority.TrimEnd('/')}/realms/HotelRealm";
        }

        var authentication = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = PlatformBearerScheme;
                options.DefaultChallengeScheme = PlatformBearerScheme;
            })
            .AddPolicyScheme(PlatformBearerScheme, "Hotel Reservation bearer tokens", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authorization = context.Request.Headers.Authorization.ToString();
                    if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return IdentityServerScheme;
                    }

                    var token = authorization["Bearer ".Length..].Trim();
                    try
                    {
                        var issuer = new JwtSecurityTokenHandler().ReadJwtToken(token).Issuer;
                        if (string.Equals(issuer, authIssuer, StringComparison.OrdinalIgnoreCase))
                        {
                            return HotelAuthScheme;
                        }

                        if (!string.IsNullOrWhiteSpace(keycloakAuthority)
                            && issuer.StartsWith(keycloakAuthority.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                        {
                            return KeycloakScheme;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Invalid tokens are sent through a real validator and rejected there.
                    }

                    return IdentityServerScheme;
                };
            })
            .AddJwtBearer(HotelAuthScheme, options =>
            {
                options.MapInboundClaims = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authIssuer,
                    ValidateAudience = true,
                    ValidAudience = authAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            })
            .AddJwtBearer(IdentityServerScheme, options =>
            {
                options.Authority = identityServerAuthority;
                options.Audience = identityServerAudience;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = identityServerAudience,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        if (!string.IsNullOrWhiteSpace(keycloakAuthority))
        {
            authentication.AddJwtBearer(KeycloakScheme, options =>
            {
                options.Authority = keycloakAuthority;
                options.Audience = keycloakAudience;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.MapInboundClaims = false;
            });
        }

        return authentication;
    }
}
