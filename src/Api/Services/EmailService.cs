using System.Net;
using System.Net.Mail;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class EmailService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmailService> _logger;

    public EmailService(AppDbContext db, ILogger<EmailService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? bcc = null)
    {
        try
        {
            var settings = await _db.AppSettings.ToDictionaryAsync(s => s.Key, s => s.Value);

            var smtpHost = settings.GetValueOrDefault("SmtpHost", "");
            var smtpPort = int.TryParse(settings.GetValueOrDefault("SmtpPort", "587"), out var p) ? p : 587;
            var smtpUser = settings.GetValueOrDefault("SmtpUser", "");
            var smtpPass = settings.GetValueOrDefault("SmtpPassword", "");
            var senderName = settings.GetValueOrDefault("SmtpSenderName", "Integraly");
            var senderEmail = settings.GetValueOrDefault("SmtpSenderEmail", "");
            var useSsl = settings.GetValueOrDefault("SmtpUseSsl", "false") == "true";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(senderEmail))
            {
                _logger.LogWarning("SMTP not configured, skipping email to {To}", to);
                return false;
            }

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.EnableSsl = useSsl;

            var message = new MailMessage();
            message.From = new MailAddress(senderEmail, senderName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            if (!string.IsNullOrEmpty(bcc))
                message.Bcc.Add(bcc);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}
