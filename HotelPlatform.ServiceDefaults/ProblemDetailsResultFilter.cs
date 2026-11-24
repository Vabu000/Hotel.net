using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HotelPlatform.ServiceDefaults;

public sealed class ProblemDetailsResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { StatusCode: >= 400 } result)
        {
            var problem = CreateProblemDetails(result, context.HttpContext);
            context.Result = new ContentResult
            {
                StatusCode = result.StatusCode,
                ContentType = "application/problem+json",
                Content = System.Text.Json.JsonSerializer.Serialize(
                    problem,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            };
        }

        await next();
    }

    public static ProblemDetails CreateProblemDetails(ObjectResult result, HttpContext context)
    {
        var status = result.StatusCode ?? StatusCodes.Status500InternalServerError;
        if (result.Value is ProblemDetails existing)
        {
            existing.Status ??= status;
            existing.Instance ??= context.Request.Path;
            existing.Extensions.TryAdd("traceId", context.TraceIdentifier);
            return existing;
        }

        if (result.Value is SerializableError serializableErrors)
        {
            var validationProblem = new ValidationProblemDetails(
                serializableErrors.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value switch
                    {
                        string[] messages => messages,
                        string message => new[] { message },
                        _ => new[] { "The supplied value is invalid." }
                    }))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };
            validationProblem.Extensions["traceId"] = context.TraceIdentifier;
            return validationProblem;
        }

        if (result.Value is ModelStateDictionary modelState)
        {
            var validationProblem = new ValidationProblemDetails(
                modelState.Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                            .ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };
            validationProblem.Extensions["traceId"] = context.TraceIdentifier;
            return validationProblem;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = GetTitle(status),
            Detail = result.Value as string,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }

    private static string GetTitle(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
        _ => "An error occurred while processing the request."
    };
}
