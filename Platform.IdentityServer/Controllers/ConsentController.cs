using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using HotelAuth.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelAuth.IdentityServer.Controllers;

[Route("consent")]
[Authorize]
public sealed class ConsentController(IIdentityServerInteractionService interaction) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return BadRequest("The authorization return URL is missing.");

        var request = await interaction.GetAuthorizationContextAsync(returnUrl);
        if (request is null) return BadRequest("The authorization request is invalid or expired.");

        return View(new ConsentViewModel
        {
            ReturnUrl = returnUrl,
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            RequestedScopes = request.ValidatedResources.RawScopeValues
                .OrderBy(scope => scope, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ConsentInputModel input)
    {
        var request = string.IsNullOrWhiteSpace(input.ReturnUrl)
            ? null
            : await interaction.GetAuthorizationContextAsync(input.ReturnUrl);
        if (request is null) return BadRequest("The authorization request is invalid or expired.");

        if (!input.Grant)
        {
            await interaction.DenyAuthorizationAsync(request, AuthorizationError.AccessDenied);
        }
        else
        {
            await interaction.GrantConsentAsync(request, new ConsentResponse
            {
                ScopesValuesConsented = request.ValidatedResources.RawScopeValues.ToArray(),
                RememberConsent = input.RememberConsent
            });
        }

        return interaction.IsValidReturnUrl(input.ReturnUrl)
            ? Redirect(input.ReturnUrl)
            : BadRequest("The authorization return URL is invalid.");
    }
}
