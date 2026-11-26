using Duende.IdentityServer.Services;
using HotelAuth.IdentityServer.Data;
using HotelAuth.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelAuth.IdentityServer.Controllers;

[Route("account")]
public sealed class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IIdentityServerInteractionService interaction) : Controller
{
    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true && IsValidReturnUrl(returnUrl))
        {
            var request = await interaction.GetAuthorizationContextAsync(returnUrl!);
            if (request is null || !request.PromptModes.Contains("login", StringComparer.Ordinal))
            {
                return Redirect(returnUrl!);
            }
        }

        var authorizationRequest = string.IsNullOrWhiteSpace(returnUrl)
            ? null
            : await interaction.GetAuthorizationContextAsync(returnUrl);

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            Username = authorizationRequest?.LoginHint
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username?.Trim();
        var user = string.IsNullOrWhiteSpace(username)
            ? null
            : await userManager.FindByEmailAsync(username) ?? await userManager.FindByNameAsync(username);

        if (user is not null)
        {
            var result = await signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                if (IsValidReturnUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl!);
                }

                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            ModelState.AddModelError(string.Empty,
                result.IsLockedOut ? "This account is locked. Try again later." : "Invalid username or password.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }

        return View(model);
    }

    [HttpGet("logout")]
    [Authorize]
    public IActionResult Logout([FromQuery] string? logoutId) => View(model: logoutId);

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutConfirmed([FromForm] string? logoutId)
    {
        await signInManager.SignOutAsync();
        var logout = await interaction.GetLogoutContextAsync(logoutId);

        if (!string.IsNullOrWhiteSpace(logout?.PostLogoutRedirectUri))
        {
            return Redirect(logout.PostLogoutRedirectUri);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private bool IsValidReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl)
        && (Url.IsLocalUrl(returnUrl) || interaction.IsValidReturnUrl(returnUrl));
}
