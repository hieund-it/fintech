using System.ComponentModel.DataAnnotations;

namespace VnStock.Application.Auth.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MinLength(2)] string DisplayName
);
