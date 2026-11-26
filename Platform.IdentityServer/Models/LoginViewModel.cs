namespace HotelAuth.IdentityServer.Models;

public sealed class LoginViewModel
{
    public string? Username { get; set; }
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
