namespace HotelAuth.IdentityServer.Models;

public sealed class ConsentViewModel
{
    public string ReturnUrl { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public IReadOnlyList<string> RequestedScopes { get; set; } = Array.Empty<string>();
    public bool RememberConsent { get; set; }
}

public sealed class ConsentInputModel
{
    public string ReturnUrl { get; set; } = string.Empty;
    public bool RememberConsent { get; set; }
    public bool Grant { get; set; }
}
