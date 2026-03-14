namespace VnStock.Application.Alerts.DTOs;

public record AlertDto(
    Guid Id,
    string Symbol,
    string Direction,   // "ABOVE" | "BELOW"
    decimal Threshold,
    bool IsActive,
    DateTime? TriggeredAt,
    DateTime CreatedAt);

public record CreateAlertRequest(
    string Symbol,
    string Direction,
    decimal Threshold);
