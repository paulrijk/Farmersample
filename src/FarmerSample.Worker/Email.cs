using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace FarmerSample.Worker;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "FarmerSample <no-reply@farmersample.local>";
    /// <summary>Where staff notifications (e.g. new sample requests) go.</summary>
    public string StaffInbox { get; set; } = "team@farmersample.local";
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string textBody, string? replyTo, CancellationToken ct);
}

/// <summary>Plain SMTP: Mailpit locally, any SMTP relay (e.g. Azure Communication Services) in the cloud.</summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> log) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string textBody, string? replyTo, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.Host))
        {
            // No mail server configured (e.g. the test site): the request is still stored and visible in the back office.
            log.LogWarning("E-mail not sent (Smtp:Host is empty): {Subject}", subject);
            return;
        }
        using var client = new SmtpClient(o.Host, o.Port) { EnableSsl = o.EnableSsl };
        if (!string.IsNullOrEmpty(o.UserName)) client.Credentials = new NetworkCredential(o.UserName, o.Password);
        using var message = new MailMessage { From = new MailAddress(o.From), Subject = subject, Body = textBody };
        message.To.Add(to);
        if (!string.IsNullOrEmpty(replyTo)) message.ReplyToList.Add(replyTo);
        await client.SendMailAsync(message, ct);
    }
}
