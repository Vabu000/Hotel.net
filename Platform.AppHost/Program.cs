Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
var builder = DistributedApplication.CreateBuilder(args);

var postgresUser = builder.AddParameter("postgres-user", "postgres", publishValueAsDefault: true);
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var sqlServerPassword = builder.AddParameter("sqlserver-password", secret: true);
var jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);
var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);
var aggregatorClientSecret = builder.AddParameter("identityserver-aggregator-secret", secret: true);

var mongodb = builder.AddMongoDB("mongo")
    .WithDataVolume("mongodb-data-v2");

var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
    .WithDataVolume("postgres-data-v2")
    .WithPgAdmin();

builder.AddContainer("sqlserver", "mcr.microsoft.com/mssql/server", "2022-latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_PID", "Developer")
    .WithEnvironment("MSSQL_SA_PASSWORD", sqlServerPassword)
    .WithVolume("sqlserver-data", "/var/opt/mssql")
    .WithEndpoint(name: "tcp", port: 1433, targetPort: 1433, scheme: "tcp");

var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithRedisCommander();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("rabbitmq-data")
    .WithManagementPlugin();

var reservationDb = postgres.AddDatabase("HotelReservationsDB");
var catalogDb = postgres.AddDatabase("HotelCatalogDB");
var identityDb = postgres.AddDatabase("IdentityDb");
var identityServerDb = postgres.AddDatabase("IdentityServerDb");
var identityServerConfigDb = postgres.AddDatabase("IdentityServerConfigDb");
var identityServerGrantsDb = postgres.AddDatabase("IdentityServerGrantsDb");
var keycloakDb = postgres.AddDatabase("keycloakdb");
var reviewsDb = mongodb.AddDatabase("mongodb");

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.0")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_URL", "jdbc:postgresql://postgres:5432/keycloakdb")
    .WithEnvironment("KC_DB_USERNAME", postgresUser)
    .WithEnvironment("KC_DB_PASSWORD", postgresPassword)
    .WithHttpEndpoint(port: 8080, targetPort: 8080)
    .WithArgs("start-dev")
    .WaitFor(keycloakDb);

var authService = builder.AddProject<Projects.Auth_Api>("auth-service")
    .WithReference(identityDb)
    .WithEnvironment("Jwt__SecretKey", jwtSigningKey)
    .WaitFor(identityDb)
    .WithHttpHealthCheck("/health/ready");

var identityServer = builder.AddProject<Projects.Platform_IdentityServer>("identityserver")
    .WithReference(identityServerDb)
    .WithReference(identityServerConfigDb)
    .WithReference(identityServerGrantsDb)
    .WithEnvironment("IdentityServer__Clients__AggregatorSecret", aggregatorClientSecret)
    .WaitFor(identityServerDb)
    .WaitFor(identityServerConfigDb)
    .WaitFor(identityServerGrantsDb)
    .WithHttpHealthCheck("/health");

var paymentsService = builder.AddProject<Projects.Payments_Api>("payments-service")
    .WithReference(identityServer)
    .WithEnvironment("Jwt__SecretKey", jwtSigningKey)
    .WithEnvironment("IdentityServer__Authority", identityServer.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WaitFor(identityServer)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

var reviewsService = builder.AddProject<Projects.HotelReviews_WebApi>("reviews-service")
    .WithReference(identityServer)
    .WithReference(reviewsDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(reviewsDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithEnvironment("Jwt__SecretKey", jwtSigningKey)
    .WithEnvironment("IdentityServer__Authority", identityServer.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WaitFor(identityServer)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

var reservationService = builder.AddProject<Projects.HotelReservation_Api>("reservation-service")
    .WithReference(identityServer)
    .WithReference(reservationDb)
    .WithReference(rabbitmq)
    .WaitFor(reservationDb)
    .WaitFor(rabbitmq)
    .WithEnvironment("Jwt__SecretKey", jwtSigningKey)
    .WithEnvironment("IdentityServer__Authority", identityServer.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WaitFor(identityServer)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

var catalogService = builder.AddProject<Projects.HotelCatalog_Api>("catalog-service")
    .WithReference(identityServer)
    .WithReference(catalogDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(catalogDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithEnvironment("Jwt__SecretKey", jwtSigningKey)
    .WithEnvironment("IdentityServer__Authority", identityServer.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WaitFor(identityServer)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

var aggregator = builder.AddProject<Projects.HotelPlatform_Aggregator>("aggregator")
    .WithReference(reviewsService)
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(reviewsService)
    .WaitFor(reservationService)
    .WaitFor(catalogService)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health/ready");

var gateway = builder.AddProject<Projects.HotelPlatform_ApiGateway>("gateway")
    .WithReference(authService)
    .WithReference(paymentsService)
    .WithReference(reviewsService)
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(aggregator)
    .WaitFor(authService)
    .WaitFor(paymentsService)
    .WaitFor(reviewsService)
    .WaitFor(reservationService)
    .WaitFor(catalogService)
    .WaitFor(aggregator)
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithHttpHealthCheck("/health/ready");

builder.Build().Run();
