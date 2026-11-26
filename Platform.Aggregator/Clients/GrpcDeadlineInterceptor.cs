using Grpc.Core;
using Grpc.Core.Interceptors;

namespace HotelPlatform.Aggregator.Clients;

internal sealed class GrpcDeadlineInterceptor(TimeSpan timeout) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        if (context.Options.Deadline is { } existingDeadline && existingDeadline < deadline)
        {
            deadline = existingDeadline;
        }

        var callOptions = context.Options.WithDeadline(deadline);
        var callContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, callOptions);
        return continuation(request, callContext);
    }
}
