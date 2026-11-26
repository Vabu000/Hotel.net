using System.Collections.Generic;
using Pulumi;
using Pulumi.Keycloak;
using Pulumi.Keycloak.Inputs;
using Pulumi.Keycloak.OpenId;
using Pulumi.Keycloak.OpenId.Inputs;

return await Deployment.RunAsync(() =>
{
    var config = new Pulumi.Config();
    var keycloakUrl = config.Get("keycloakUrl") ?? "http://localhost:8080";
    var adminUser = config.Get("adminUser") ?? "admin";
    var adminPassword = config.RequireSecret("adminPassword");
    var aggregatorClientSecret = config.RequireSecret("aggregatorClientSecret");

    var provider = new Provider("keycloak", new ProviderArgs
    {
        Url = keycloakUrl,
        ClientId = "admin-cli",
        Username = adminUser,
        Password = adminPassword
    });

    var customOptions = new CustomResourceOptions { Provider = provider };

    // 1. Realm: HotelRealm
    var realm = new Realm("hotel-realm", new RealmArgs
    {
        RealmName = "HotelRealm",
        Enabled = true,
        DisplayName = "Hotel Reservation Platform",
        RegistrationAllowed = true,
        ResetPasswordAllowed = true,
        RememberMe = true,
        LoginWithEmailAllowed = true,
        DuplicateEmailsAllowed = false
    }, customOptions);

    // 2. Realm Roles
    var adminRole = new Role("role-admin", new RoleArgs { RealmId = realm.Id, Name = "admin" }, customOptions);
    var managerRole = new Role("role-manager", new RoleArgs { RealmId = realm.Id, Name = "manager" }, customOptions);
    var userRole = new Role("role-user", new RoleArgs { RealmId = realm.Id, Name = "user" }, customOptions);
    var guestRole = new Role("role-guest", new RoleArgs { RealmId = realm.Id, Name = "guest" }, customOptions);

    // 3. Resource Servers (Authorization enabled)
    var reservationsResourceServer = CreateResourceServer(
        "reservations-api", realm.Id,
        ["reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve"],
        customOptions);

    var catalogResourceServer = CreateResourceServer(
        "catalog-api", realm.Id,
        ["catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish"],
        customOptions);

    var paymentsResourceServer = CreateResourceServer(
        "payments-api", realm.Id,
        ["payments:read", "payments:process", "payments:refund"],
        customOptions);
    var reviewsResourceServer = CreateResourceServer(
        "reviews-api", realm.Id,
        ["reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve"],
        customOptions);

    // 4. Role policies and scope permissions for all resource servers.
    AddRolePermissions("reservations", realm.Id, reservationsResourceServer, adminRole.Id,
        "admin", ["reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve"], customOptions);
    AddRolePermissions("catalog", realm.Id, catalogResourceServer, adminRole.Id,
        "admin", ["catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish"], customOptions);
    AddRolePermissions("payments", realm.Id, paymentsResourceServer, adminRole.Id,
        "admin", ["payments:read", "payments:process", "payments:refund"], customOptions);
    AddRolePermissions("reviews", realm.Id, reviewsResourceServer, adminRole.Id,
        "admin", ["reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve"], customOptions);

    AddRolePermissions("reservations", realm.Id, reservationsResourceServer, managerRole.Id,
        "manager", ["reservations:read", "reservations:update", "reservations:approve"], customOptions);
    AddRolePermissions("catalog", realm.Id, catalogResourceServer, managerRole.Id,
        "manager", ["catalog:read", "catalog:update", "catalog:publish"], customOptions);
    AddRolePermissions("reviews", realm.Id, reviewsResourceServer, managerRole.Id,
        "manager", ["reviews:read", "reviews:update", "reviews:approve"], customOptions);

    AddRolePermissions("reservations", realm.Id, reservationsResourceServer, userRole.Id,
        "user", ["reservations:read", "reservations:create"], customOptions);
    AddRolePermissions("catalog", realm.Id, catalogResourceServer, userRole.Id,
        "user", ["catalog:read"], customOptions);
    AddRolePermissions("reviews", realm.Id, reviewsResourceServer, userRole.Id,
        "user", ["reviews:read", "reviews:create"], customOptions);

    AddRolePermissions("catalog", realm.Id, catalogResourceServer, guestRole.Id,
        "guest", ["catalog:read"], customOptions);
    AddRolePermissions("reviews", realm.Id, reviewsResourceServer, guestRole.Id,
        "guest", ["reviews:read"], customOptions);

    // 5. Clients (Authorization Code Flow + Client Credentials Flow)
    var postmanClient = CreateAuthCodeClient("reservations.postman", realm.Id,
        ["https://oauth.pstmn.io/v1/callback"], secret: null, pkce: true, customOptions);

    var swaggerClient = CreateAuthCodeClient("reservations.swagger", realm.Id,
        ["http://localhost:5000/swagger/oauth2-redirect.html", "https://localhost:5001/swagger/oauth2-redirect.html"],
        secret: null, pkce: true, customOptions);

    var aggregatorClient = CreateClientCredentialsClient("aggregator.service", realm.Id,
        aggregatorClientSecret, customOptions);

    // 6. Test Users per Role
    CreateUserWithRole("admin-user", realm.Id, "alice.admin", "alice.admin@hotel.local", config.RequireSecret("testUsers:adminPassword"), adminRole.Id, customOptions);
    CreateUserWithRole("manager-user", realm.Id, "john.manager", "john.manager@hotel.local", config.RequireSecret("testUsers:managerPassword"), managerRole.Id, customOptions);
    CreateUserWithRole("standard-user", realm.Id, "bob.user", "bob.user@hotel.local", config.RequireSecret("testUsers:userPassword"), userRole.Id, customOptions);
    CreateUserWithRole("guest-user", realm.Id, "guest.demo", "guest.demo@hotel.local", config.RequireSecret("testUsers:guestPassword"), guestRole.Id, customOptions);

    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = Output.Format($"{keycloakUrl}/realms/HotelRealm"),
        ["AdminConsoleUrl"] = $"{keycloakUrl}/admin",
        ["ClientIds"] = Output.All(postmanClient.ClientId, swaggerClient.ClientId, aggregatorClient.ClientId),
        ["ResourceServerIds"] = Output.All(
            reservationsResourceServer.Client.ResourceServerId,
            catalogResourceServer.Client.ResourceServerId,
            paymentsResourceServer.Client.ResourceServerId,
            reviewsResourceServer.Client.ResourceServerId)
    };
});

static (Client Client, Dictionary<string, ClientAuthorizationScope> Scopes) CreateResourceServer(
    string clientId,
    Input<string> realmId,
    string[] scopes,
    CustomResourceOptions opts)
{
    var client = new Client($"rs-{clientId}", new ClientArgs
    {
        RealmId = realmId,
        ClientId = clientId,
        Name = $"{clientId} Resource Server",
        AccessType = "CONFIDENTIAL",
        ServiceAccountsEnabled = true,
        Authorization = new ClientAuthorizationArgs
        {
            PolicyEnforcementMode = "ENFORCING",
            DecisionStrategy = "AFFIRMATIVE"
        }
    }, opts);

    var scopeResources = new Dictionary<string, ClientAuthorizationScope>();
    foreach (var scope in scopes)
    {
        scopeResources[scope] = new ClientAuthorizationScope($"scope-{clientId}-{scope.Replace(':', '-')}", new ClientAuthorizationScopeArgs
        {
            RealmId = realmId,
            ResourceServerId = client.ResourceServerId,
            Name = scope
        }, opts);
    }

    return (client, scopeResources);
}

static void AddRolePermissions(
    string resourceName,
    Input<string> realmId,
    (Client Client, Dictionary<string, ClientAuthorizationScope> Scopes) resourceServer,
    Input<string> roleId,
    string roleName,
    string[] scopes,
    CustomResourceOptions opts)
{
    var policy = CreateRolePolicy(
        $"{resourceName}-{roleName}-policy",
        realmId,
        resourceServer.Client.ResourceServerId,
        roleId,
        opts);

    _ = new ClientAuthorizationPermission($"{resourceName}-{roleName}-scope-permission", new ClientAuthorizationPermissionArgs
    {
        RealmId = realmId,
        ResourceServerId = resourceServer.Client.ResourceServerId,
        Name = $"{roleName} {resourceName} scopes",
        Type = "scope",
        DecisionStrategy = "AFFIRMATIVE",
        Scopes = scopes.Select(scope => resourceServer.Scopes[scope].Name).ToArray(),
        Policies = { policy.Id }
    }, opts);
}

static ClientRolePolicy CreateRolePolicy(
    string name,
    Input<string> realmId,
    Input<string> resourceServerId,
    Input<string> roleId,
    CustomResourceOptions opts)
{
    return new ClientRolePolicy(name, new ClientRolePolicyArgs
    {
        RealmId = realmId,
        ResourceServerId = resourceServerId,
        Name = name,
        DecisionStrategy = "UNANIMOUS",
        Logic = "POSITIVE",
        Roles =
        {
            new ClientRolePolicyRoleArgs { Id = roleId, Required = true }
        }
    }, opts);
}

static Client CreateAuthCodeClient(
    string clientId,
    Input<string> realmId,
    string[] redirectUris,
    Input<string>? secret,
    bool pkce,
    CustomResourceOptions opts)
{
    return new Client($"client-{clientId}", new ClientArgs
    {
        RealmId = realmId,
        ClientId = clientId,
        Name = clientId,
        AccessType = pkce ? "PUBLIC" : "CONFIDENTIAL",
        ClientSecret = secret,
        StandardFlowEnabled = true,
        DirectAccessGrantsEnabled = true,
        ValidRedirectUris = redirectUris,
        PkceCodeChallengeMethod = pkce ? "S256" : string.Empty
    }, opts);
}

static Client CreateClientCredentialsClient(
    string clientId,
    Input<string> realmId,
    Input<string> secret,
    CustomResourceOptions opts)
{
    return new Client($"client-{clientId}", new ClientArgs
    {
        RealmId = realmId,
        ClientId = clientId,
        Name = clientId,
        AccessType = "CONFIDENTIAL",
        ClientSecret = secret,
        ServiceAccountsEnabled = true,
        StandardFlowEnabled = false
    }, opts);
}

static void CreateUserWithRole(
    string resourceName,
    Input<string> realmId,
    string username,
    string email,
    Input<string> password,
    Input<string> roleName,
    CustomResourceOptions opts)
{
    var user = new User(resourceName, new UserArgs
    {
        RealmId = realmId,
        Username = username,
        Email = email,
        EmailVerified = true,
        Enabled = true,
        InitialPassword = new UserInitialPasswordArgs
        {
            Value = password,
            Temporary = true
        }
    }, opts);

    _ = new UserRoles($"{resourceName}-roles", new UserRolesArgs
    {
        RealmId = realmId,
        UserId = user.Id,
        RoleIds = { roleName }
    }, opts);
}
