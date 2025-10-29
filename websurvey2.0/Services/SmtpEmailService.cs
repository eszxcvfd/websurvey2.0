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

        try
        {
            _logger.LogInformation("Attempting to send email to {To} with subject '{Subject}'", toEmail, subject);
            
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Timeout = 30000 // 30 seconds timeout
            };

            if (!string.IsNullOrWhiteSpace(_settings.User))
            {
                client.Credentials = new NetworkCredential(_settings.User, _settings.Password);
                _logger.LogDebug("Using SMTP authentication with user: {User}", _settings.User);
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
            _logger.LogInformation("Email sent successfully to {To}", toEmail);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error sending email to {To}. StatusCode: {StatusCode}", 
                toEmail, smtpEx.StatusCode);
            throw new InvalidOperationException($"Failed to send email: {smtpEx.Message}", smtpEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {To}", toEmail);
            throw;
        }
    }
}