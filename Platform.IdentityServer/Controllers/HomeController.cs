using Duende.IdentityServer.Services;
using HotelAuth.IdentityServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelAuth.IdentityServer.Controllers;

public sealed class HomeController(IIdentityServerInteractionService interaction) : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => Content("Hotel IdentityServer is ready.");

    [HttpGet("/error")]
    public async Task<IActionResult> Error([FromQuery] string? errorId)
    {
        var error = string.IsNullOrWhiteSpace(errorId)
            ? null
            : await interaction.GetErrorContextAsync(errorId);
        return View(new ErrorViewModel
        {
            Message = error?.ErrorDescription ?? error?.Error ?? "The sign-in request could not be completed."
        });
    }
}
