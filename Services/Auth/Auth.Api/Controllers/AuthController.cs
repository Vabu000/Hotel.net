using HotelAuth.Api.Data;
using HotelAuth.Api.Models;
using HotelAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelAuth.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    AuthDbContext db,
    IJwtTokenService jwtService,
    IEmailService emailService) : ControllerBase
{
    public sealed record RegisterRequest(string Username, string Email, string Password, string? FullName);
    public sealed record LoginRequest(string EmailOrUsername, string Password);
    public sealed record VerifyEmailRequest(string UserId, string Token);
    public sealed record ResendVerificationRequest(string Email);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public sealed record TokenRequest(string? RefreshToken);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName ?? request.Username
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await userManager.AddToRoleAsync(user, "User");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendAsync(user.Email!, "Verify your email", $"Confirmation token: {token}", ct);

        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = user.Id }, new
        {
            user.Id,
            user.UserName,
            user.Email,
            message = "Registration successful. Please verify your email before logging in."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.EmailOrUsername)
            ?? await userManager.FindByNameAsync(request.EmailOrUsername);

        if (user is null)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        if (!user.EmailConfirmed)
        {
            return Unauthorized(new { error = "Email is not confirmed" });
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return StatusCode(StatusCodes.Status423Locked, new { error = "Account is locked" });
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized(new { error = "Invalid credentials" });
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, roles);
        var refreshToken = jwtService.GenerateRefreshToken(user.Id);

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        SetRefreshCookie(refreshToken.Token, refreshToken.ExpiresAt);
        return Ok(new { accessToken, expiresInSeconds = 900 });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequest? body, CancellationToken ct)
    {
        var tokenValue = Request.Cookies["refreshToken"] ?? body?.RefreshToken;
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            return Unauthorized(new { error = "Refresh token is missing" });
        }

        var existing = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == tokenValue, ct);

        if (existing is null || !existing.IsActive)
        {
            return Unauthorized(new { error = "Invalid or expired refresh token" });
        }

        var replacement = jwtService.GenerateRefreshToken(existing.UserId);
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByToken = replacement.Token;

        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);

        var roles = await userManager.GetRolesAsync(existing.User);
        var accessToken = jwtService.GenerateAccessToken(existing.User, roles);

        SetRefreshCookie(replacement.Token, replacement.ExpiresAt);
        return Ok(new { accessToken, expiresInSeconds = 900 });
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] TokenRequest? body, CancellationToken ct)
    {
        var tokenValue = Request.Cookies["refreshToken"] ?? body?.RefreshToken;
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var existing = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == tokenValue, ct);
        if (existing is null || !existing.IsActive)
        {
            return NotFound(new { error = "Active refresh token not found" });
        }

        existing.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var tokenValue = Request.Cookies["refreshToken"];
        if (!string.IsNullOrWhiteSpace(tokenValue))
        {
            var existing = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == tokenValue, ct);
            if (existing is not null && existing.IsActive)
            {
                existing.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return NotFound();

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded ? Ok(new { verified = true }) : BadRequest(result.Errors);
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await emailService.SendAsync(user.Email!, "Verify your email", $"Confirmation token: {token}", ct);
        }

        return Ok(new { message = "If the account exists and is unverified, a verification email has been sent." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await emailService.SendAsync(user.Email!, "Reset password", $"Reset token: {token}", ct);
        }

        return Ok(new { message = "If the account exists, password reset instructions have been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return BadRequest(new { error = "Invalid reset request" });

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        // Revoke all active refresh tokens after password reset
        var activeTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var rt in activeTokens)
        {
            rt.RevokedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    private void SetRefreshCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });
    }
}
