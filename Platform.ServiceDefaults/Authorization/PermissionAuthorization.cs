using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace HotelPlatform.ServiceDefaults.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = permission;
    }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Support Keycloak RPT 'authorization' JSON claim
        var authClaim = context.User.FindFirst("authorization")?.Value;
        if (!string.IsNullOrWhiteSpace(authClaim))
        {
            try
            {
                using var doc = JsonDocument.Parse(authClaim);
                if (doc.RootElement.TryGetProperty("permissions", out var permsElement))
                {
                    foreach (var resource in permsElement.EnumerateArray())
                    {
                        if (resource.TryGetProperty("scopes", out var scopesElement))
                        {
                            foreach (var scope in scopesElement.EnumerateArray())
                            {
                                if (string.Equals(scope.GetString(), requirement.Permission, StringComparison.OrdinalIgnoreCase))
                                {
                                    context.Succeed(requirement);
                                    return Task.CompletedTask;
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed authorization JSON claim
            }
        }

        // Also support flat scope/permission claims (IdentityServer / custom JWT)
        var hasScope = context.User.Claims.Any(c =>
            (c.Type is "scope" or "permissions") &&
            c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase));

        if (hasScope)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class PermissionAuthorizationExtensions
{
    public static IServiceCollection AddPermissionPolicies(
        this IServiceCollection services,
        params string[] permissions)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var permission in permissions)
            {
                options.AddPolicy(permission, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new PermissionRequirement(permission));
                });
            }
        });

        return services;
    }
}
