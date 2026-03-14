using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using VnStock.Application.Alerts.Services;

namespace VnStock.Infrastructure.Email;

/// <summary>
/// Sends price alert emails via SMTP (MailKit).
/// Config keys: Email:SmtpHost, Email:SmtpPort, Email:Username, Email:Password, Email:FromAddress.
/// Compatible with Brevo, Gmail, SendGrid SMTP relays.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAlertEmailAsync(
        string toEmail, string symbol, string direction,
        decimal threshold, decimal currentPrice, CancellationToken ct = default)
    {
        var host = _config["Email:SmtpHost"];
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var from = _config["Email:FromAddress"] ?? "noreply@vnstock.local";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Email not configured. Skipping alert email for {Symbol}.", symbol);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"[VnStock Alert] {symbol} price {direction.ToLower()} {threshold:N0} VND";
        message.Body = new TextPart("html")
        {
            Text = BuildEmailBody(symbol, direction, threshold, currentPrice)
        };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            _logger.LogInformation("Alert email sent to {Email} for {Symbol}.", toEmail, symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email to {Email} for {Symbol}.", toEmail, symbol);
        }
    }

    private static string BuildEmailBody(string symbol, string direction, decimal threshold, decimal currentPrice)
        => $"""
            <html><body style="font-family:sans-serif;padding:24px">
              <h2 style="color:#1e40af">VnStock Price Alert Triggered</h2>
              <p>Your price alert for <strong>{symbol}</strong> has been triggered.</p>
              <table style="border-collapse:collapse;margin-top:12px">
                <tr><td style="padding:6px 16px 6px 0;color:#6b7280">Condition</td>
                    <td><strong>{symbol} {direction} {threshold:N0} VND</strong></td></tr>
                <tr><td style="padding:6px 16px 6px 0;color:#6b7280">Current Price</td>
                    <td><strong style="color:#dc2626">{currentPrice:N0} VND</strong></td></tr>
              </table>
              <p style="margin-top:24px;color:#6b7280;font-size:12px">
                You can manage your alerts in the VnStock dashboard.
              </p>
            </body></html>
            """;
}
