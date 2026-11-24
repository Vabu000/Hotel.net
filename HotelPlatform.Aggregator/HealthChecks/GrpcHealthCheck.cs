using Microsoft.Extensions.Diagnostics.HealthChecks;
using Grpc.Core;
using Grpc.Health.V1;

namespace HotelPlatform.Aggregator.HealthChecks;

public class GrpcHealthCheck : IHealthCheck
{
    private readonly string _serviceName;
    private readonly string _grpcServiceUrl;
    private readonly ILogger<GrpcHealthCheck> _logger;

    public GrpcHealthCheck(string serviceName, string grpcServiceUrl, ILogger<GrpcHealthCheck> logger)
    {
        _serviceName = serviceName;
        _grpcServiceUrl = grpcServiceUrl;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = Grpc.Net.Client.GrpcChannel.ForAddress(_grpcServiceUrl);

            var healthClient = new Health.HealthClient(channel);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var request = new HealthCheckRequest { Service = "" };
            var response = await healthClient.CheckAsync(request, cancellationToken: cts.Token);

            return response.Status switch
            {
                HealthCheckResponse.Types.ServingStatus.Serving => HealthCheckResult.Healthy(
                    $"{_serviceName} is healthy"),
                HealthCheckResponse.Types.ServingStatus.NotServing => HealthCheckResult.Unhealthy(
                    $"{_serviceName} is not serving"),
                _ => HealthCheckResult.Degraded(
                    $"{_serviceName} status unknown")
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            _logger.LogWarning("{ServiceName}: gRPC Health Protocol not implemented, performing connectivity check", _serviceName);
            return await PerformConnectivityCheck(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ServiceName} health check failed: {Message}", _serviceName, ex.Message);
            return HealthCheckResult.Unhealthy(
                $"{_serviceName} is unreachable",
                ex);
        }
    }

    private async Task<HealthCheckResult> PerformConnectivityCheck(CancellationToken cancellationToken)
    {
        try
        {
            using var channel = Grpc.Net.Client.GrpcChannel.ForAddress(_grpcServiceUrl);

            await channel.ConnectAsync(cancellationToken);
            var state = channel.State;

            return state == Grpc.Core.ConnectivityState.Ready
                ? HealthCheckResult.Healthy($"{_serviceName} is reachable (connectivity check)")
                : HealthCheckResult.Degraded($"{_serviceName} connectivity state is {state}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ServiceName} connectivity check failed", _serviceName);
            return HealthCheckResult.Unhealthy($"{_serviceName} is unreachable", ex);
        }
    }
}
