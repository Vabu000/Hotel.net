using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace HotelPlatform.ServiceDefaults.Grpc;

public sealed class GrpcLoggingInterceptor : Interceptor
{
    private readonly ILogger<GrpcLoggingInterceptor> _logger;

    public GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            var response = await continuation(request, context).ConfigureAwait(false);
            _logger.LogInformation(
                "gRPC {Method} completed in {ElapsedMilliseconds} ms",
                context.Method,
                System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return response;
        }
        catch (RpcException exception)
        {
            _logger.LogWarning(exception, "gRPC {Method} failed with {StatusCode}",
                context.Method, exception.StatusCode);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "gRPC {Method} failed", context.Method);
            throw;
        }
    }
}
