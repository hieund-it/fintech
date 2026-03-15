using System.ComponentModel.DataAnnotations;

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
    [Required, MinLength(2), MaxLength(10)] string Symbol,
    [Required] string Direction,
    [Range(0.01, 1_000_000_000.0)] decimal Threshold);
