using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace HotelAuth.Api.Services;

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Hotel Reservation System";
}

public sealed class SmtpEmailService(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpEmailOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(toEmail));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        await client.SendMailAsync(message, ct);
        logger.LogInformation("Sent authentication email {Subject} to {Email}", subject, toEmail);
    }
}
