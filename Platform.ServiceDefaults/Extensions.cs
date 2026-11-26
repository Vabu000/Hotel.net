using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using HotelPlatform.ServiceDefaults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using HotelPlatform.ServiceDefaults.Events;
using MassTransit;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.Services.AddDefaultHealthChecks();

        builder.ConfigureSerilog();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
        builder.Services.AddTransient<ProblemDetailsResultFilter>();
        builder.Services.Configure<MvcOptions>(options =>
            options.Filters.AddService<ProblemDetailsResultFilter>());

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromMilliseconds(200);
                options.Retry.UseJitter = true;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(10);
            });

            http.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
            http.AddServiceDiscovery();
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var isHealthRequest = context.Request.Path.StartsWithSegments("/health")
                    || context.Request.Path.StartsWithSegments("/alive");
                var client = isHealthRequest
                    ? "health"
                    : context.Connection.RemoteIpAddress?.ToString() ?? "local";

                return RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = isHealthRequest ? 1_000 : 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Request limit exceeded",
                    Detail = "Wait briefly before sending another request."
                }, cancellationToken);
            };
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Polly")
                    .AddMeter("HotelCatalog")
                    .AddMeter("HotelCatalog.Api")
                    .AddMeter("HotelPlatform.Aggregator")
                    .AddMeter("HotelPlatform.Reservation");
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddSource("Npgsql")
                    .AddSource("HotelReviews.MongoDB")
                    .AddSource("Platform.Messaging");
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging =>
                logging.AddOtlpExporter());

            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
                metrics.AddOtlpExporter());

            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
                tracing.AddOtlpExporter());
        }

        return builder;
    }

    public static IHostApplicationBuilder ConfigureSerilog(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.With(new ActivityTraceEnricher())
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.WithProperty("MachineName", System.Environment.MachineName)
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter()),
            writeToProviders: true);
        return builder;
    }

    public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("ready", () => HealthCheckResult.Healthy(), ["ready"]);

        return services;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthReport
        });
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = WriteHealthReport
        });

        return app;
    }

    private static async Task WriteHealthReport(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration,
                tags = entry.Value.Tags,
                data = entry.Value.Data
            })
        });
        await context.Response.WriteAsync(result);
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
        app.UseRateLimiter();

        app.MapDefaultEndpoints();

        return app;
    }

    /// <summary>
    /// Adds MassTransit with RabbitMQ for event publishing
    /// </summary>
    public static IServiceCollection AddEventPublisher(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionName = builder.Configuration.GetConnectionString("rabbitmq");

                if (!string.IsNullOrEmpty(connectionName))
                {
                    cfg.Host(connectionName);
                }
                else
                {
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });
                }

                cfg.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

        return services;
    }
}
