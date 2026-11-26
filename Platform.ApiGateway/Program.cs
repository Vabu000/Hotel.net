using HotelPlatform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseServiceDefaults();

app.Use(async (context, next) =>
{
    var target = ResolveTarget(context.Request.Path.Value);
    var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();

    try
    {
        await next();

        if (target is not null)
        {
            app.Logger.LogInformation(
                "Gateway routed {Method} {Path} to {TargetService}; status {StatusCode}; duration {DurationMs} ms",
                context.Request.Method,
                context.Request.Path,
                target,
                context.Response.StatusCode,
                System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }
    catch (Exception exception)
    {
        app.Logger.LogError(
            exception,
            "Gateway routing to {TargetService} failed for {Method} {Path} after {DurationMs} ms",
            target ?? "unmatched",
            context.Request.Method,
            context.Request.Path,
            System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        throw;
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/swagger/reservations.json", "Reservations API");
        options.SwaggerEndpoint("/swagger/catalog.json", "Catalog API");
        options.SwaggerEndpoint("/swagger/reviews.json", "Reviews API");
        options.SwaggerEndpoint("/swagger/payments.json", "Payments API");
        options.SwaggerEndpoint("/swagger/aggregator.json", "Aggregator API");
        options.OAuthClientId("swagger");
        options.OAuthUsePkce();
        options.OAuthScopes(
            "openid", "profile", "roles",
            "reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve",
            "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish",
            "reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve",
            "payments:read", "payments:process", "payments:refund");
    });
}

app.MapGet("/", () => Results.Json(new
{
    service = "Hotel Platform API Gateway",
    version = "1.0",
    status = "Running",
    description = "YARP Reverse Proxy - всі запити автоматично перенаправляються",
    availableRoutes = new[]
    {
        new { path = "/api/reviews", proxiedTo = "reviews-service" },
        new { path = "/api/Requests", proxiedTo = "reviews-service" },
        new { path = "/api/Service, /api/RoomCategory, and /api/DiscountCategory", proxiedTo = "catalog-service" },
        new { path = "/api/Client, /api/Room, /api/Payment, and /api/Reservation", proxiedTo = "reservation-service" },
        new { path = "/api/Payments", proxiedTo = "payments-service" },
        new { path = "/api/RoomAggregator", proxiedTo = "aggregator" }
    },
    testing = new
    {
        message = "Використовуй прямі HTTP запити",
        examples = new[]
        {
            "GET http://localhost:5000/api/reviews",
            "GET http://localhost:5000/api/Reservation",
            "GET http://localhost:5000/health"
        }
    },
    dashboard = "https://localhost:17050"
})).ExcludeFromDescription();

app.MapReverseProxy();

app.Run();

static string? ResolveTarget(string? path)
{
    if (path is null) return null;
    if (path.StartsWith("/api/RoomAggregator", StringComparison.OrdinalIgnoreCase)) return "aggregator";
    if (path.StartsWith("/api/reviews", StringComparison.OrdinalIgnoreCase)) return "reviews-service";
    if (path.StartsWith("/api/Requests", StringComparison.OrdinalIgnoreCase)) return "reviews-service";
    if (path.StartsWith("/api/Payments", StringComparison.OrdinalIgnoreCase)) return "payments-service";
    if (path.StartsWith("/api/Service", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/RoomCategory", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/DiscountCategory", StringComparison.OrdinalIgnoreCase)) return "catalog-service";
    if (path.StartsWith("/api/Reservation", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/Client", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/Room", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/Payment", StringComparison.OrdinalIgnoreCase)) return "reservation-service";
    return null;
}
