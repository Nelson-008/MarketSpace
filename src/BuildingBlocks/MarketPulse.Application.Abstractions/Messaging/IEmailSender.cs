namespace MarketPulse.Application.Abstractions.Messaging;

public sealed record EmailMessage(string To, string Subject, string TextBody);

/// <summary>Outgoing email. Development uses SMTP to Mailpit; the production provider is an open decision.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
