using HotelPlatform.ServiceDefaults.Authorization;
using HotelReservation.Api.Grpc;
using HotelReservation.Api.IntegrationEvents;
using HotelReservation.Api.Metrics;
using HotelReservation.Bll.Profiles;
using HotelReservation.Bll.Services;
using HotelReservation.Dal.Interfaces;
using HotelReservation.Dal.Repositories;
using HotelReservation.Api.HealthChecks;
using HotelReservation.Dal.Database;
using Microsoft.Extensions.Hosting;
using HotelPlatform.ServiceDefaults.Authentication;
using HotelPlatform.ServiceDefaults.Swagger;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Configure Kestrel to support both HTTP/1.1 and HTTP/2 for gRPC and health checks
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddHealthChecks()
    .AddCheck<ReservationDatabaseHealthCheck>(
        "reservation-database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready"]);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddPlatformOAuth(
    builder.Configuration,
    "openid", "profile", "roles",
    "reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve",
    "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish",
    "payments:read", "payments:process", "payments:refund"));

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<ReservationDatabaseInitializer>();
builder.Services.AddSingleton<ReservationMetrics>();
builder.Services.AddSingleton<ReservationApiMetrics>();

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddPlatformAuthentication(
    builder.Configuration,
    builder.Environment,
    identityServerAudience: "reservations_api",
    keycloakAudience: "reservations-api");

builder.Services.AddPermissionPolicies(
    "reservations:read",
    "reservations:create",
    "reservations:update",
    "reservations:delete",
    "reservations:approve",
    "catalog:read",
    "catalog:create",
    "catalog:update",
    "catalog:delete",
    "catalog:publish",
    "payments:read",
    "payments:process",
    "payments:refund");

// The integration-test host replaces business services and does not need a broker connection.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddEventPublisher(builder);
    builder.AddRabbitMQClient("rabbitmq");
    builder.Services.AddHostedService<CatalogSyncConsumer>();
}

var app = builder.Build();
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.GetRequiredService<ReservationDatabaseInitializer>().InitializeAsync();
}
app.UseServiceDefaults();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<GrpcReservationsService>();
app.MapGrpcHealthChecksService();
app.MapControllers();

app.Run();

public partial class Program { }
