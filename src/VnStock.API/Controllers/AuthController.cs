using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VnStock.Application.Auth.DTOs;
using VnStock.Domain.Interfaces;

namespace VnStock.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var (user, accessToken, refreshToken) = await _authService.RegisterAsync(
                request.Email, request.Password, request.DisplayName, ct);
            SetRefreshTokenCookie(refreshToken);
            return Ok(new AuthResponse(user.Id.ToString(), user.Email!, user.DisplayName, accessToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var (user, accessToken, refreshToken) = await _authService.LoginAsync(
                request.Email, request.Password, ct);
            SetRefreshTokenCookie(refreshToken);
            return Ok(new AuthResponse(user.Id.ToString(), user.Email!, user.DisplayName, accessToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid credentials." });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = "No refresh token." });

        try
        {
            var (accessToken, newRefreshToken) = await _authService.RefreshAsync(refreshToken, ct);
            SetRefreshTokenCookie(newRefreshToken);
            return Ok(new { accessToken });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid or expired refresh token." });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.LogoutAsync(refreshToken, ct);

        Response.Cookies.Delete("refresh_token");
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        userId = User.FindFirst("sub")?.Value,
        email = User.FindFirst("email")?.Value,
        displayName = User.FindFirst("displayName")?.Value
    });

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }
}
