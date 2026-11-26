using System.Net.Http.Headers;
using System.Text.Json;

namespace HotelPlatform.Aggregator.Services;

public sealed class IdentityServerTokenProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<IdentityServerTokenProvider> logger)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
            {
                return _accessToken;
            }

            var authority = configuration["IdentityServer:Authority"]?.TrimEnd('/');
            var clientId = configuration["IdentityServer:Clients:AggregatorClientId"];
            var clientSecret = configuration["IdentityServer:Clients:AggregatorSecret"];
            if (string.IsNullOrWhiteSpace(authority)
                || string.IsNullOrWhiteSpace(clientId)
                || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException("IdentityServer authority and aggregator client credentials must be configured.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{authority}/connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["scope"] = "reservations:read catalog:read reviews:read"
                })
            };

            using var response = await httpClientFactory.CreateClient("IdentityServerToken")
                .SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("IdentityServer client-credentials request failed with {StatusCode}: {ResponseBody}",
                    response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            _accessToken = document.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("IdentityServer returned an empty access token.");
            var lifetime = document.RootElement.TryGetProperty("expires_in", out var expiresIn)
                ? expiresIn.GetInt32()
                : 300;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, lifetime));
            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}

public sealed class IdentityServerAuthorizationHandler(IdentityServerTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
