using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace websurvey2._0.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.From))
        {
            _logger.LogWarning("Email settings are not configured. Email to {To} with subject '{Subject}' not sent. Body: {Body}",
                toEmail, subject, htmlBody);
            return; // Dev fallback: log only
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_settings.User))
        {
            client.Credentials = new NetworkCredential(_settings.User, _settings.Password);
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(_settings.From!, _settings.FromName ?? _settings.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(toEmail);

        await client.SendMailAsync(msg, ct);
    }
}