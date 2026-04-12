using DkpSystem.Models;
using Xunit;

namespace DkpSystem.Tests;

/// <summary>
/// Unit tests for model classes to verify properties exist with correct types.
/// </summary>
public class ModelTests
{
    /// <summary>
    /// Verifies that the Guild model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void Guild_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var guild = new Guild
        {
            Id = Guid.NewGuid(),
            Name = "Test Guild",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<Guid>(guild.Id);
        Assert.IsType<string>(guild.Name);
        Assert.IsType<DateTime>(guild.CreatedAt);
    }

    /// <summary>
    /// Verifies that the User model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void User_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            Role = "raider",
            GuildId = Guid.NewGuid(),
            DkpBalance = 100,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<Guid>(user.Id);
        Assert.IsType<string>(user.Email);
        Assert.IsType<string>(user.Username);
        Assert.IsType<string>(user.PasswordHash);
        Assert.IsType<string>(user.Role);
        Assert.True(user.GuildId == null || user.GuildId is Guid);
        Assert.IsType<int>(user.DkpBalance);
        Assert.IsType<bool>(user.Active);
        Assert.IsType<DateTime>(user.CreatedAt);
    }

    /// <summary>
    /// Verifies that the Event model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void Event_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var eventModel = new Event
        {
            Id = Guid.NewGuid(),
            GuildId = Guid.NewGuid(),
            Name = "Test Event",
            Description = "Test Description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<Guid>(eventModel.Id);
        Assert.IsType<Guid>(eventModel.GuildId);
        Assert.IsType<string>(eventModel.Name);
        Assert.True(eventModel.Description == null || eventModel.Description is string);
        Assert.IsType<Guid>(eventModel.CreatedBy);
        Assert.IsType<DateTime>(eventModel.CreatedAt);
    }

    /// <summary>
    /// Verifies that the EventRewardLine model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void EventRewardLine_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var rewardLine = new EventRewardLine
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Reason = "Kill dragon",
            DkpAmount = 15
        };

        // Assert
        Assert.IsType<Guid>(rewardLine.Id);
        Assert.IsType<Guid>(rewardLine.EventId);
        Assert.IsType<string>(rewardLine.Reason);
        Assert.IsType<int>(rewardLine.DkpAmount);
    }

    /// <summary>
    /// Verifies that the DkpEarning model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void DkpEarning_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var earning = new DkpEarning
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            RewardLineId = Guid.NewGuid(),
            DkpAmount = 20,
            EarnedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<Guid>(earning.Id);
        Assert.IsType<Guid>(earning.UserId);
        Assert.IsType<Guid>(earning.EventId);
        Assert.IsType<Guid>(earning.RewardLineId);
        Assert.IsType<int>(earning.DkpAmount);
        Assert.IsType<DateTime>(earning.EarnedAt);
    }

    /// <summary>
    /// Verifies that the Auction model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void Auction_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            GuildId = Guid.NewGuid(),
            Name = "Test Auction",
            Status = "pending",
            ClosesAt = DateTime.UtcNow.AddHours(1),
            ClosedAt = null,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<Guid>(auction.Id);
        Assert.IsType<Guid>(auction.GuildId);
        Assert.IsType<string>(auction.Name);
        Assert.IsType<string>(auction.Status);
        Assert.IsType<DateTime>(auction.ClosesAt);
        Assert.True(auction.ClosedAt == null || auction.ClosedAt is DateTime);
        Assert.IsType<Guid>(auction.CreatedBy);
        Assert.IsType<DateTime>(auction.CreatedAt);
    }

    /// <summary>
    /// Verifies that the AuctionItem model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void AuctionItem_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var item = new AuctionItem
        {
            Id = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            Name = "Epic Sword",
            MinimumBid = 50,
            Delivered = false,
            DeliveredAt = null,
            DeliveredBy = null,
            WinnerId = null,
            FinalPrice = null,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<Guid>(item.Id);
        Assert.IsType<Guid>(item.AuctionId);
        Assert.IsType<string>(item.Name);
        Assert.IsType<int>(item.MinimumBid);
        Assert.IsType<bool>(item.Delivered);
        Assert.True(item.DeliveredAt == null || item.DeliveredAt is DateTime);
        Assert.True(item.DeliveredBy == null || item.DeliveredBy is Guid);
        Assert.True(item.WinnerId == null || item.WinnerId is Guid);
        Assert.True(item.FinalPrice == null || item.FinalPrice is int);
        Assert.IsType<DateTime>(item.CreatedAt);
    }

    /// <summary>
    /// Verifies that the AuctionBid model has all required properties with correct types.
    /// </summary>
    [Fact]
    public void AuctionBid_HasAllPropertiesWithCorrectTypes()
    {
        // Arrange & Act
        var bid = new AuctionBid
        {
            Id = Guid.NewGuid(),
            AuctionItemId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 100,
            BidType = "main",
            PlacedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<Guid>(bid.Id);
        Assert.IsType<Guid>(bid.AuctionItemId);
        Assert.IsType<Guid>(bid.UserId);
        Assert.IsType<int>(bid.Amount);
        Assert.IsType<string>(bid.BidType);
        Assert.IsType<DateTime>(bid.PlacedAt);
        Assert.IsType<DateTime>(bid.UpdatedAt);
    }
}
