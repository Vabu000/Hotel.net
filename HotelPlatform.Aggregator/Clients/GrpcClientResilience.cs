using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace HotelPlatform.Aggregator.Clients;

internal static class GrpcClientResilience
{
    public static ServiceConfig ForReadMethods(
        string service,
        int maxAttempts,
        TimeSpan initialBackoff,
        TimeSpan maxBackoff,
        params string[] methods)
    {
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = maxAttempts,
            InitialBackoff = initialBackoff,
            MaxBackoff = maxBackoff,
            BackoffMultiplier = 2,
            RetryableStatusCodes = { StatusCode.Unavailable }
        };
        var methodConfig = new MethodConfig
        {
            RetryPolicy = retryPolicy
        };
        foreach (var method in methods)
        {
            methodConfig.Names.Add(new MethodName { Service = service, Method = method });
        }

        return new ServiceConfig { MethodConfigs = { methodConfig } };
    }
}
