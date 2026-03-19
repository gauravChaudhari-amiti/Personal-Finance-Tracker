using System.Net;
using System.Net.Mail;

namespace PersonalFinanceTracker.Api.Services;

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendAsync(string toEmail, string subject, string htmlBody, string plainTextBody);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Email:FromEmail"]);

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(
                "Email delivery is not configured. Subject: {Subject}, Recipient: {Recipient}, Body: {Body}",
                subject,
                toEmail,
                plainTextBody);
            return;
        }

        var smtpHost = _configuration["Email:SmtpHost"]!;
        var smtpPort = int.TryParse(_configuration["Email:SmtpPort"], out var parsedPort) ? parsedPort : 587;
        var smtpUsername = _configuration["Email:SmtpUsername"];
        var smtpPassword = _configuration["Email:SmtpPassword"];
        var fromEmail = _configuration["Email:FromEmail"]!;
        var fromName = _configuration["Email:FromName"] ?? "Personal Finance Tracker";
        var useSsl = !bool.TryParse(_configuration["Email:UseSsl"], out var parsedUseSsl) || parsedUseSsl;

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain"));

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = useSsl
        };

        if (!string.IsNullOrWhiteSpace(smtpUsername))
        {
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
        }

        await client.SendMailAsync(message);
    }
}
