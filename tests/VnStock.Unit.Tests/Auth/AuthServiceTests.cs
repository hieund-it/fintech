using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using VnStock.Application.Auth.Services;
using VnStock.Domain.Entities;
using VnStock.Domain.Interfaces;
using Xunit;

namespace VnStock.Unit.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IAuthDbContext> _dbContextMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _tokenServiceMock = new Mock<ITokenService>();
        _dbContextMock = new Mock<IAuthDbContext>();

        var refreshTokens = new List<RefreshToken>();
        var mockDbSet = CreateMockDbSet(refreshTokens);
        _dbContextMock.Setup(d => d.RefreshTokens).Returns(mockDbSet.Object);
        _dbContextMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
        _tokenServiceMock.Setup(t => t.HashToken(It.IsAny<string>())).Returns("hashed");

        _sut = new AuthService(_userManagerMock.Object, _tokenServiceMock.Object, _dbContextMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        _userManagerMock.Setup(u => u.FindByEmailAsync("dupe@test.com"))
            .ReturnsAsync(new ApplicationUser());

        await _sut.Invoking(s => s.RegisterAsync("dupe@test.com", "Pass1!", "Name"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorized()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(u => u.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        await _sut.Invoking(s => s.LoginAsync("test@test.com", "wrong"))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test" };
        _userManagerMock.Setup(u => u.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, "Pass1!")).ReturnsAsync(true);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("access-token");

        var (_, accessToken, refreshToken) = await _sut.LoginAsync("test@test.com", "Pass1!");

        accessToken.Should().Be("access-token");
        refreshToken.Should().Be("refresh-token");
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mock = new Mock<DbSet<T>>();
        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        mock.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(data.Add);
        return mock;
    }
}
