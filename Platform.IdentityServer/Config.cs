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
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("hotel-api", "Hotel Platform API"),
            new ApiScope("hotel-reservation", "Hotel Reservation Service"),
            new ApiScope("hotel-catalog", "Hotel Catalog Service"),
            new ApiScope("hotel-reviews", "Hotel Reviews Service"),
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            new ApiResource("hotel-api", "Hotel Platform API")
            {
                Scopes = { "hotel-api", "hotel-reservation", "hotel-catalog", "hotel-reviews" }
            }
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // Machine-to-machine client (service accounts)
            new Client
            {
                ClientId = "hotel-aggregator",
                ClientName = "Hotel Aggregator Service",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("aggregator-secret".Sha256()) },
                AllowedScopes = { "hotel-api", "hotel-reservation", "hotel-catalog" }
            },

            // Interactive client for SPA / Swagger UI
            new Client
            {
                ClientId = "hotel-swagger",
                ClientName = "Hotel Swagger UI",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = true,
                RedirectUris = {
                    "https://localhost:7001/swagger/oauth2-redirect.html",
                    "https://localhost:7002/swagger/oauth2-redirect.html",
                    "https://localhost:7003/swagger/oauth2-redirect.html",
                },
                PostLogoutRedirectUris = { "https://localhost:7001/swagger/" },
                AllowedCorsOrigins = {
                    "https://localhost:7001",
                    "https://localhost:7002",
                    "https://localhost:7003",
                },
                AllowedScopes = { "openid", "profile", "email", "hotel-api" },
                AllowOfflineAccess = true,
                AccessTokenLifetime = 3600,
            },

            // Resource Owner Password (for testing)
            new Client
            {
                ClientId = "hotel-test-client",
                ClientName = "Hotel Test Client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("test-secret".Sha256()) },
                AllowedScopes = { "openid", "profile", "email", "hotel-api" },
                AllowOfflineAccess = true,
            },
        };
}
