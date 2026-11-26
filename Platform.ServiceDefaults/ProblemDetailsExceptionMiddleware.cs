using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotelPlatform.ServiceDefaults;

public sealed class ProblemDetailsExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ProblemDetailsExceptionMiddleware(
        RequestDelegate next,
        ILogger<ProblemDetailsExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request was cancelled by the client: {Path}", context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "An exception occurred after the response started");
                throw;
            }

            _logger.LogError(exception, "Unhandled request exception for {Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var status = exception switch
        {
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            TimeoutException or HttpRequestException or OperationCanceledException =>
                (int)HttpStatusCode.ServiceUnavailable,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = status switch
            {
                (int)HttpStatusCode.BadRequest => "Bad Request",
                (int)HttpStatusCode.NotFound => "Not Found",
                (int)HttpStatusCode.ServiceUnavailable => "Service Unavailable",
                _ => "Internal Server Error"
            },
            Detail = _environment.IsDevelopment()
                || status is (int)HttpStatusCode.BadRequest or (int)HttpStatusCode.NotFound
                ? exception.Message
                : status == (int)HttpStatusCode.ServiceUnavailable
                    ? "A dependent service is temporarily unavailable."
                    : "An unexpected error occurred.",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: new JsonSerializerOptions(JsonSerializerDefaults.Web),
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }
}
