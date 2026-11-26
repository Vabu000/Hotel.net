using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace HotelReservation.Api.Metrics;

public sealed class ReservationMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ReservationApiMetrics metrics)
    {
        var routeTemplate = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
        var method = context.Request.Method;
        var stopwatch = Stopwatch.StartNew();

        using var activeOperation = metrics.TrackActiveOperation();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusClass = $"{context.Response.StatusCode / 100}xx";
            metrics.RecordRequest(routeTemplate, method, statusClass, stopwatch.Elapsed.TotalMilliseconds);

            if (statusClass == "2xx" && method == HttpMethods.Post &&
                routeTemplate.EndsWith("Reservation", StringComparison.OrdinalIgnoreCase))
            {
                metrics.RecordCreated(statusClass);
            }

            if (statusClass == "2xx" && (method == HttpMethods.Put || method == HttpMethods.Post) &&
                (routeTemplate.Contains("/status", StringComparison.OrdinalIgnoreCase) ||
                 routeTemplate.Contains("/checkin", StringComparison.OrdinalIgnoreCase) ||
                 routeTemplate.Contains("/checkout", StringComparison.OrdinalIgnoreCase) ||
                 routeTemplate.Contains("/cancel", StringComparison.OrdinalIgnoreCase)))
            {
                metrics.RecordStatusChange(statusClass);
            }
        }
    }
}
