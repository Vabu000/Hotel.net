using Duende.IdentityServer.Models;

namespace HotelAuth.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
            new IdentityResource("roles", "User roles", ["role"])
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("reservations:read", "Read reservations"),
            new ApiScope("reservations:create", "Create reservations"),
            new ApiScope("reservations:update", "Update reservations"),
            new ApiScope("reservations:delete", "Delete reservations"),
            new ApiScope("reservations:approve", "Approve and complete reservations"),
            new ApiScope("catalog:read", "Read room catalog"),
            new ApiScope("catalog:create", "Create catalog entries"),
            new ApiScope("catalog:update", "Update catalog entries"),
            new ApiScope("catalog:delete", "Delete catalog entries"),
            new ApiScope("catalog:publish", "Publish catalog changes"),
            new ApiScope("reviews:read", "Read guest reviews"),
            new ApiScope("reviews:create", "Create guest reviews"),
            new ApiScope("reviews:update", "Update guest reviews"),
            new ApiScope("reviews:delete", "Delete guest reviews"),
            new ApiScope("reviews:approve", "Approve guest reviews"),
            new ApiScope("payments:read", "Read payments"),
            new ApiScope("payments:process", "Process payments"),
            new ApiScope("payments:refund", "Refund payments")
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            new ApiResource("reservations_api", "Hotel Reservations API")
            {
                Scopes = { "reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve", "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish", "payments:read", "payments:process", "payments:refund" },
                UserClaims = { "role" }
            },
            new ApiResource("catalog_api", "Hotel Catalog API")
            {
                Scopes = { "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish" },
                UserClaims = { "role" }
            },
            new ApiResource("payments_api", "Hotel Payments API")
            {
                Scopes = { "payments:read", "payments:process", "payments:refund" },
                UserClaims = { "role" }
            },
            new ApiResource("reviews_api", "Hotel Reviews API")
            {
                Scopes = { "reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve" },
                UserClaims = { "role" }
            }
        };

    public static IEnumerable<Client> GetClients(string aggregatorClientSecret) =>
        new Client[]
        {
            // 1. Postman — Authorization Code Flow
            new Client
            {
                ClientId = "postman",
                ClientName = "Postman OAuth Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = true,
                RequireConsent = true,
                RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
                AllowedScopes = { "openid", "profile", "email", "roles", "reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve", "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish", "reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve", "payments:read", "payments:process", "payments:refund" },
                AllowOfflineAccess = true,
                AccessTokenLifetime = 15 * 60,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Absolute,
                AbsoluteRefreshTokenLifetime = 7 * 24 * 60 * 60
            },

            // 2. Aggregator — Client Credentials Flow (service-to-service)
            new Client
            {
                ClientId = "aggregator_service",
                ClientName = "Hotel Platform Aggregator",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret(aggregatorClientSecret.Sha256()) },
                AllowedScopes = { "reservations:read", "catalog:read", "reviews:read" }
            },

            // 3. Swagger UI — Public Client with PKCE
            new Client
            {
                ClientId = "swagger",
                ClientName = "Hotel Swagger UI",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                RequireConsent = true,
                RedirectUris =
                {
                    "http://localhost:5000/swagger/oauth2-redirect.html"
                },
                AllowedCorsOrigins = { "http://localhost:5000" },
                AllowedScopes = { "openid", "profile", "roles", "reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve", "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish", "reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve", "payments:read", "payments:process", "payments:refund" },
                AllowOfflineAccess = true,
                AccessTokenLifetime = 15 * 60,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Absolute,
                AbsoluteRefreshTokenLifetime = 7 * 24 * 60 * 60
            }
        };
}
