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
    private static readonly IReadOnlyDictionary<string, HashSet<string>> RolePermissions =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve",
                "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish",
                "reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve",
                "payments:read", "payments:process", "payments:refund"
            },
            ["manager"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "reservations:read", "reservations:update", "reservations:approve",
                "catalog:read", "catalog:update", "catalog:publish",
                "reviews:read", "reviews:update", "reviews:approve"
            },
            ["user"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "reservations:read", "reservations:create", "catalog:read", "reviews:read", "reviews:create"
            },
            ["guest"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "catalog:read", "reviews:read"
            }
        };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!IsRoleAllowed(context.User, requirement.Permission))
        {
            return Task.CompletedTask;
        }

        // Support Keycloak RPT 'authorization' JSON claim
        var authClaim = context.User.FindFirst("authorization")?.Value;
        if (!string.IsNullOrWhiteSpace(authClaim))
        {
            try
            {
                using var doc = JsonDocument.Parse(authClaim);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("permissions", out var permsElement)
                    && permsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var resource in permsElement.EnumerateArray())
                    {
                        if (resource.ValueKind == JsonValueKind.Object
                            && resource.TryGetProperty("scopes", out var scopesElement)
                            && scopesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var scope in scopesElement.EnumerateArray())
                            {
                                if (scope.ValueKind == JsonValueKind.String
                                    && string.Equals(scope.GetString(), requirement.Permission, StringComparison.OrdinalIgnoreCase))
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

    private static bool IsRoleAllowed(System.Security.Claims.ClaimsPrincipal user, string permission)
    {
        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in user.Claims.Where(claim =>
                     claim.Type is System.Security.Claims.ClaimTypes.Role or "role"))
        {
            roleNames.Add(role.Value);
        }

        foreach (var claim in user.FindAll("realm_access"))
        {
            try
            {
                using var document = JsonDocument.Parse(claim.Value);
                if (document.RootElement.TryGetProperty("roles", out var roles)
                    && roles.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        if (role.GetString() is { } roleName) roleNames.Add(roleName);
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed role JSON; permission checks remain denied.
            }
        }

        if (roleNames.Count == 0)
        {
            // IdentityServer client-credentials tokens have their client ID as the subject and no user role.
            var clientId = user.FindFirst("client_id")?.Value;
            var subject = user.FindFirst("sub")?.Value;
            return !string.IsNullOrWhiteSpace(clientId)
                && string.Equals(clientId, subject, StringComparison.Ordinal);
        }

        return roleNames.Any(roleName => RolePermissions.TryGetValue(roleName, out var permissions)
            && permissions.Contains(permission));
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
