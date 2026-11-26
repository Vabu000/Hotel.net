using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotelAuth.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelAuth.Api.Services;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "HotelAuth.Api";
    public string Audience { get; set; } = "hotel-platform";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    RefreshToken GenerateRefreshToken(string userId);
    ClaimsPrincipal? ValidateToken(string token);
}

public sealed class JwtTokenService(IOptions<JwtSettings> options) : IJwtTokenService
{
    private readonly JwtSettings _settings = options.Value;

    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roleNames = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var role in roleNames)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roleNames)
        {
            var rolePermissions = role.ToLowerInvariant() switch
            {
                "admin" => new[]
                {
                    "reservations:read", "reservations:create", "reservations:update", "reservations:delete", "reservations:approve",
                    "catalog:read", "catalog:create", "catalog:update", "catalog:delete", "catalog:publish",
                    "reviews:read", "reviews:create", "reviews:update", "reviews:delete", "reviews:approve",
                    "payments:read", "payments:process", "payments:refund"
                },
                "manager" => new[]
                {
                    "reservations:read", "reservations:update", "reservations:approve",
                    "catalog:read", "catalog:update", "catalog:publish",
                    "reviews:read", "reviews:update", "reviews:approve"
                },
                "user" => new[] { "reservations:read", "reservations:create", "catalog:read", "reviews:read", "reviews:create" },
                "guest" => new[] { "catalog:read", "reviews:read" },
                _ => Array.Empty<string>()
            };

            permissions.UnionWith(rolePermissions);
        }

        if (permissions.Count > 0)
        {
            claims.Add(new Claim("scope", string.Join(' ', permissions)));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(string userId)
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return new RefreshToken
        {
            Token = Convert.ToBase64String(bytes),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays)
        };
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);

        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}

public sealed class MockEmailService(ILogger<MockEmailService> logger) : IEmailService
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("Mock email to {Email} | Subject: {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
