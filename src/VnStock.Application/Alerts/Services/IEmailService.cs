namespace VnStock.Application.Alerts.Services;

public interface IEmailService
{
    Task SendAlertEmailAsync(string toEmail, string symbol, string direction, decimal threshold, decimal currentPrice, CancellationToken ct = default);
}
