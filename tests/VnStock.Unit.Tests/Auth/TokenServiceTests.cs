using FluentAssertions;
using Microsoft.Extensions.Configuration;
using VnStock.Application.Auth.Services;
using VnStock.Domain.Entities;
using Xunit;

namespace VnStock.Unit.Tests.Auth;

public class TokenServiceTests
{
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-min-32-characters-long-xxxx",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();
        _sut = new TokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", DisplayName = "Test" };
        var token = _sut.GenerateAccessToken(user);
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturn64ByteBase64()
    {
        var token = _sut.GenerateRefreshToken();
        token.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(token).Should().HaveCount(64);
    }

    [Fact]
    public void HashToken_SameInput_ReturnsSameHash()
    {
        var h1 = _sut.HashToken("my-token");
        var h2 = _sut.HashToken("my-token");
        h1.Should().Be(h2);
    }

    [Fact]
    public void HashToken_DifferentInput_ReturnsDifferentHash()
    {
        _sut.HashToken("token-a").Should().NotBe(_sut.HashToken("token-b"));
    }
}
