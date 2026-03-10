namespace VnStock.Application.Auth.DTOs;

public record AuthResponse(
    string UserId,
    string Email,
    string DisplayName,
    string AccessToken
);
