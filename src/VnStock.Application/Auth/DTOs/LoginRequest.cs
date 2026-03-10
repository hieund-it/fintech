using System.ComponentModel.DataAnnotations;

namespace VnStock.Application.Auth.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
