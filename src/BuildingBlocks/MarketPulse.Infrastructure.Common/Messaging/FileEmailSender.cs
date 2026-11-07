using System.Globalization;
using MarketPulse.Application.Abstractions.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Common.Messaging;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Smtp" (Mailpit in dev, real provider later) or "File" (development without Docker).</summary>
    public string Mode { get; set; } = "Smtp";

    /// <summary>Directory for <c>File</c> mode. Must be outside source control (.local/ is git-ignored).</summary>
    public string FileDirectory { get; set; } = ".local/mail";
}

/// <summary>Development-only sender that writes each email to a text file instead of sending it.</summary>
public sealed class FileEmailSender(IOptions<EmailOptions> options, IHostEnvironment environment) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Relative paths are anchored to the app's content root, not the process working directory.
        var directory = Path.GetFullPath(options.Value.FileDirectory, environment.ContentRootPath);
        Directory.CreateDirectory(directory);
        var name = string.Create(CultureInfo.InvariantCulture, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.txt");
        var content = $"To: {message.To}\nSubject: {message.Subject}\n\n{message.TextBody}\n";
        await File.WriteAllTextAsync(Path.Combine(directory, name), content, cancellationToken).ConfigureAwait(false);
    }
}
