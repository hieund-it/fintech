using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using VnStock.API.Hubs;

namespace VnStock.Unit.Tests.Hubs;

/// <summary>Tests for MarketHub subscribe/unsubscribe group management.</summary>
public class MarketHubTests
{
    private static MarketHub CreateHub(
        out Mock<IGroupManager> groupsMock,
        string connectionId = "conn-1")
    {
        groupsMock = new Mock<IGroupManager>();
        groupsMock
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groupsMock
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        var hub = new MarketHub
        {
            Context = contextMock.Object,
            Groups = groupsMock.Object,
        };
        return hub;
    }

    [Fact]
    public async Task SubscribeSymbol_AddsConnectionToUppercaseGroup()
    {
        var hub = CreateHub(out var groups);

        await hub.SubscribeSymbol("vcb");

        groups.Verify(g =>
            g.AddToGroupAsync("conn-1", "VCB", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsubscribeSymbol_RemovesConnectionFromGroup()
    {
        var hub = CreateHub(out var groups);
        await hub.SubscribeSymbol("vcb");

        await hub.UnsubscribeSymbol("vcb");

        groups.Verify(g =>
            g.RemoveFromGroupAsync("conn-1", "VCB", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubscribeSymbol_BeyondMaxLimit_ThrowsHubException()
    {
        var hub = CreateHub(out _, "conn-limit");

        // Subscribe up to the max (50)
        for (int i = 0; i < 50; i++)
            await hub.SubscribeSymbol($"SYM{i:00}");

        // 51st subscribe should throw
        var act = async () => await hub.SubscribeSymbol("OVER");
        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*Max 50*");
    }

    [Fact]
    public async Task SubscribeSymbol_AfterUnsubscribe_AllowsResubscribe()
    {
        var hub = CreateHub(out var groups, "conn-resub");

        // Fill to max
        for (int i = 0; i < 50; i++)
            await hub.SubscribeSymbol($"SYM{i:00}");

        // Unsubscribe one, then resubscribe should succeed
        await hub.UnsubscribeSymbol("SYM00");
        var act = async () => await hub.SubscribeSymbol("NEW1");
        await act.Should().NotThrowAsync();

        groups.Verify(g =>
            g.AddToGroupAsync("conn-resub", "NEW1", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
