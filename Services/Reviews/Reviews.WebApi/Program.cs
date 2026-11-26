using FluentValidation;
using HotelReviews.Application.Behaviors;
using HotelReviews.Infrastructure;
using HotelReviews.Infrastructure.Persistence;
using HotelReviews.Infrastructure.Seeding;
using HotelReviews.Infrastructure.Persistence.Indexes;
using HotelReviews.WebApi.Middleware;
using MediatR;
using Serilog;
using System.Reflection;
using HotelReviews.WebApi.Services;
using HotelReviews.WebApi.HealthChecks;
using HotelReviews.Application.Common.Interfaces;
using HotelReviews.Infrastructure.Configuration;
using HotelPlatform.ServiceDefaults.Authentication;
using HotelPlatform.ServiceDefaults.Authorization;
using HotelReviews.WebApi.IntegrationEvents;
using MongoDB.Bson;
using MongoDB.Driver;
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPlatformAuthentication(
    builder.Configuration,
    builder.Environment,
    identityServerAudience: "reviews_api",
    keycloakAudience: "reviews-api");
builder.Services.AddPermissionPolicies(
    "reviews:read",
    "reviews:create",
    "reviews:update",
    "reviews:delete",
    "reviews:approve");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; 
    options.CompactionPercentage = 0.25; 
});

builder.AddRedisClient("redis");
builder.Services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache, RedisDistributedCache>();

builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
builder.Services.AddSingleton<ITwoLevelCacheService, TwoLevelCacheService>();

builder.Services.AddEventPublisher(builder);
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddHostedService<ReviewsCatalogSyncConsumer>();

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<ICacheService, CacheService>();

builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        Assembly.Load("HotelReviews.Application"));
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

builder.Services.AddValidatorsFromAssembly(
    Assembly.Load("HotelReviews.Application"),
    includeInternalTypes: false);

builder.Services.AddAutoMapper(
    Assembly.Load("HotelReviews.Application"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hotel Reviews API",
        Version = "v1",
        Description = "Clean Architecture API for managing hotel reviews and requests"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<MongoDatabaseHealthCheck>(
        "reviews-mongodb",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready", "database", "mongodb"])
    .AddCheck<ReviewsRedisHealthCheck>(
        "reviews-redis-cache",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: ["ready", "cache", "redis"]);

var app = builder.Build();

app.UseServiceDefaults();

try
{
    Log.Information("Initializing MongoDB...");

    using (var scope = app.Services.CreateScope())
    {
        var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        await EnsureMongoDatabaseReadyAsync(
            mongoContext,
            app.Logger,
            app.Lifetime.ApplicationStopping);

        var indexCreator = scope.ServiceProvider.GetRequiredService<MongoIndexCreator>();
        await indexCreator.CreateIndexesAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
    }

    Log.Information("MongoDB initialized successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to initialize MongoDB");
    throw;
}

app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Reviews API v1");
        options.RoutePrefix = "swagger"; 
    });
}

//app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.MapGrpcService<GrpcReviewsService>();
app.MapGrpcHealthChecksService();
app.MapControllers();


try
{
    Log.Information("Starting Hotel Reviews API...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static async Task EnsureMongoDatabaseReadyAsync(
    MongoDbContext context,
    Microsoft.Extensions.Logging.ILogger logger,
    CancellationToken cancellationToken)
{
    const int maxAttempts = 15;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await context.Database.RunCommandAsync(
                (Command<BsonDocument>)"{ping:1}",
                cancellationToken: cancellationToken);
            return;
        }
        catch (MongoException ex) when (
            attempt < maxAttempts
            && ex is MongoConnectionException)
        {
            logger.LogWarning(
                ex,
                "Reviews MongoDB is not ready; retrying connection ({Attempt}/{MaxAttempts})",
                attempt,
                maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    throw new InvalidOperationException("Could not connect to the Reviews MongoDB database");
}
