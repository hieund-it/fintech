using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
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
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }

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
        displayName = User.FindFirst("displayName")?.Value,
        avatarUrl = User.FindFirst("avatarUrl")?.Value
    });

    [HttpGet("oauth/{provider}")]
    [AllowAnonymous]
    public IActionResult OAuthChallenge(string provider)
    {
        var scheme = GetAuthScheme(provider);
        if (scheme is null) return BadRequest(new { error = $"Unsupported provider: {provider}" });

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { provider }),
            Items = { ["LoginProvider"] = provider }
        };
        return Challenge(properties, scheme);
    }

    [HttpGet("oauth/{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback(string provider, CancellationToken ct)
    {
        var scheme = GetAuthScheme(provider);
        if (scheme is null) return BadRequest(new { error = $"Unsupported provider: {provider}" });

        var result = await HttpContext.AuthenticateAsync(scheme);
        if (!result.Succeeded || result.Principal is null)
            return Redirect(GetFrontendErrorUrl("Authentication failed"));

        var claims = result.Principal.Claims.ToList();
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Name || c.Type == ClaimTypes.GivenName)?.Value;
        var providerKey = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var avatarUrl = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value
            ?? claims.FirstOrDefault(c => c.Type == "urn:github:avatar_url")?.Value;

        if (providerKey is null)
            return Redirect(GetFrontendErrorUrl("Authentication failed: missing user identifier"));
        if (email is null)
            return Redirect(GetFrontendErrorUrl(
                provider.Equals("github", StringComparison.OrdinalIgnoreCase)
                    ? "Your GitHub account has no verified public email. Please add one in GitHub Settings and try again."
                    : "Email not available from provider"));

        try
        {
            var (_, accessToken, refreshToken) = await _authService.ExternalLoginAsync(
                provider, providerKey, email, name ?? email, avatarUrl, ct);

            SetRefreshTokenCookie(refreshToken, SameSiteMode.Lax);

            var frontendUrl = GetValidatedFrontendCallbackUrl();
            return Redirect($"{frontendUrl}?token={Uri.EscapeDataString(accessToken)}");
        }
        catch (InvalidOperationException ex)
        {
            return Redirect(GetFrontendErrorUrl(ex.Message));
        }
    }

    private static string? GetAuthScheme(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => GoogleDefaults.AuthenticationScheme,
        "github" => GitHubAuthenticationDefaults.AuthenticationScheme,
        _ => null
    };

    /// Returns the configured frontend callback URL after validating its host is in the CORS allow-list.
    /// Prevents open redirect if the config value is misconfigured or injected.
    private string GetValidatedFrontendCallbackUrl()
    {
        var url = _config["OAuth:FrontendCallbackUrl"];
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException("OAuth:FrontendCallbackUrl is not configured");

        var allowedOrigins = (_config["Cors:Origins"] ?? "http://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
            !allowedOrigins.Any(o => Uri.TryCreate(o, UriKind.Absolute, out var origin) &&
                                     origin.Host == parsed.Host && origin.Port == parsed.Port))
        {
            throw new InvalidOperationException($"OAuth:FrontendCallbackUrl '{url}' is not in the CORS allow-list");
        }

        return url;
    }

    private string GetFrontendErrorUrl(string error)
    {
        try
        {
            var baseUrl = GetValidatedFrontendCallbackUrl();
            return $"{baseUrl}?error={Uri.EscapeDataString(error)}";
        }
        catch
        {
            // Fallback to a relative error path if config is broken — avoids leaking token to wrong host
            return $"/login?error={Uri.EscapeDataString(error)}";
        }
    }

    private void SetRefreshTokenCookie(string token, SameSiteMode sameSite = SameSiteMode.Strict)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = sameSite,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }
}
