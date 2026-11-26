using System.Security.Claims;
using HotelAuth.Api.Data;
using HotelAuth.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelAuth.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public sealed class UsersController(
    UserManager<ApplicationUser> userManager,
    AuthDbContext db) : ControllerBase
{
    public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber);
    public sealed record AssignRoleRequest(string Role);

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.UserName, u.Email, u.FullName, u.EmailConfirmed, u.CreatedAt })
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!IsOwnerOrAdmin(id)) return Forbid();
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new { user.Id, user.UserName, user.Email, user.FullName, user.EmailConfirmed, roles });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfile(string id, UpdateProfileRequest request)
    {
        if (!IsOwnerOrAdmin(id)) return Forbid();
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpGet("{id}/refresh-tokens")]
    public async Task<IActionResult> GetRefreshTokens(string id, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(id)) return Forbid();
        var tokens = await db.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.UserId == id)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => new { rt.Id, rt.CreatedAt, rt.ExpiresAt, rt.RevokedAt, rt.IsActive })
            .ToListAsync(ct);
        return Ok(tokens);
    }

    [HttpGet("{id}/roles")]
    public async Task<IActionResult> GetRoles(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(await userManager.GetRolesAsync(user));
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(string id, AssignRoleRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await userManager.AddToRoleAsync(user, request.Role);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole(string id, string role)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await userManager.RemoveFromRoleAsync(user, role);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    private bool IsOwnerOrAdmin(string targetUserId)
    {
        if (User.IsInRole("Admin")) return true;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return string.Equals(currentUserId, targetUserId, StringComparison.Ordinal);
    }
}
