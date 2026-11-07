using System.Net.Mail;
using MarketPulse.Application.Abstractions.Messaging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Common.Messaging;

public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public bool EnableSsl { get; set; }

    public string From { get; set; } = "no-reply@marketpulse.local";
}

/// <summary>
/// Plain SMTP sender. In development it points at Mailpit (docker-compose), which captures mail and delivers nothing.
/// The production email provider is an open decision (decision log).
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var o = options.Value;
        using var client = new SmtpClient(o.Host, o.Port) { EnableSsl = o.EnableSsl };
        using var mail = new MailMessage(o.From, message.To, message.Subject, message.TextBody);
        await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
    }
}
