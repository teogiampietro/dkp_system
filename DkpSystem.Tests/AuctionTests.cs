using Xunit;
using Dapper;
using DkpSystem.Data;
using DkpSystem.Data.Repositories;
using DkpSystem.Services;
using DkpSystem.Models;

namespace DkpSystem.Tests;

/// <summary>
/// Unit tests for Module 4 - Item Auctions.
/// </summary>
public class AuctionTests : IAsyncLifetime
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly AuctionRepository _auctionRepository;
    private readonly BidRepository _bidRepository;
    private readonly UserRepository _userRepository;
    private readonly AuctionService _auctionService;
    private Guid _testGuildId;
    private Guid _testAdminId;
    private Guid _testRaider1Id;
    private Guid _testRaider2Id;

    public AuctionTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
                               ?? "Host=localhost;Port=5433;Database=dkp_test;Username=postgres;Password=postgres";
        _connectionFactory = new DbConnectionFactory(connectionString);
        _auctionRepository = new AuctionRepository(_connectionFactory);
        _bidRepository = new BidRepository(_connectionFactory);
        _userRepository = new UserRepository(_connectionFactory);
        _auctionService = new AuctionService(_auctionRepository, _bidRepository, _userRepository, new AuctionNotificationService());
    }

    public async Task InitializeAsync()
    {
        // Create test guild
        _testGuildId = Guid.NewGuid();
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(
            "INSERT INTO guilds (id, name, invitation_code, created_at) VALUES (@Id, @Name, @InvitationCode, @CreatedAt)",
            new { Id = _testGuildId, Name = "Test Guild", InvitationCode = Guid.NewGuid().ToString("N")[..8].ToUpper(), CreatedAt = DateTime.UtcNow }
        );

        // Create test users
        _testAdminId = await CreateTestUser("admin@test.com", "Admin", "admin", 1000);
        _testRaider1Id = await CreateTestUser("raider1@test.com", "Raider1", "raider", 500);
        _testRaider2Id = await CreateTestUser("raider2@test.com", "Raider2", "raider", 300);
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM auction_bids WHERE auction_item_id IN (SELECT id FROM auction_items WHERE auction_id IN (SELECT id FROM auctions WHERE guild_id = @GuildId))", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM auction_items WHERE auction_id IN (SELECT id FROM auctions WHERE guild_id = @GuildId)", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM auctions WHERE guild_id = @GuildId", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM users WHERE guild_id = @GuildId", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM guilds WHERE id = @GuildId", new { GuildId = _testGuildId });
    }

    private async Task<Guid> CreateTestUser(string email, string username, string role, int dkpBalance)
    {
        var userId = Guid.NewGuid();
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO users (id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at)
              VALUES (@Id, @Email, @Username, @PasswordHash, @Role, @GuildId, @DkpBalance, @Active, @CreatedAt)",
            new
            {
                Id = userId,
                Email = email,
                Username = username,
                PasswordHash = "dummy_hash",
                Role = role,
                GuildId = _testGuildId,
                DkpBalance = dkpBalance,
                Active = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        return userId;
    }

    [Fact]
    public async Task CreateAuction_WithDuplicateItemNames_IsRejected()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)>
        {
            ("Sword", 10, null),
            ("Shield", 15, null),
            ("Sword", 20, null) // Duplicate
        };

        // Act
        var result = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Duplicate item names", result.ErrorMessage);
    }

    [Fact]
    public async Task StartAuction_SetsStatusToOpenAndPreservesClosesAt()
    {
        // Arrange
        var expectedClosesAt = DateTime.UtcNow.AddMinutes(30);
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", expectedClosesAt, _testAdminId, items);
        Assert.True(createResult.Success);
        var auctionId = createResult.Auction!.Id;

        // Act
        var result = await _auctionService.StartAuctionAsync(auctionId);

        // Assert
        Assert.True(result.Success);
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        Assert.NotNull(auction);
        Assert.Equal("open", auction.Status);
        Assert.True(Math.Abs((auction.ClosesAt - expectedClosesAt.ToUniversalTime()).TotalSeconds) < 2);
    }

    [Fact]
    public async Task CloseAuctionEarly_SetsStatusToClosedImmediately()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);

        // Act
        var result = await _auctionService.CloseAuctionAsync(auctionId);

        // Assert
        Assert.True(result.Success);
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        Assert.NotNull(auction);
        Assert.Equal("closed", auction.Status);
        Assert.NotNull(auction.ClosedAt);
    }

    [Fact]
    public async Task PlaceBid_BelowMinimumBid_IsRejected()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 50, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Act
        var result = await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 30, "main");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("at least 50", result.ErrorMessage);
    }

    [Fact]
    public async Task PlaceBid_ThatExceedsRaiderBalance_IsRejected()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Act - Raider2 has 300 DKP, try to bid 400
        var result = await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, itemId, 400, "main");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("exceed your balance", result.ErrorMessage);
    }

    [Fact]
    public async Task PlaceBid_WhereTotalActiveBidsExceedBalance_IsRejected()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)>
        {
            ("Sword", 10, null),
            ("Shield", 10, null)
        };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = (await _auctionRepository.GetAuctionItemsAsync(auctionId)).ToList();

        // Act - Raider2 has 300 DKP, bid 200 on first item, then try 150 on second (total 350)
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, auctionItems[0].Id, 200, "main");
        var result = await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, auctionItems[1].Id, 150, "main");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("exceed your balance", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateBid_WhileAuctionIsOpen_Succeeds()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Place initial bid
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 50, "main");

        // Act - Update bid (same type, higher amount — winners may not downgrade bid type)
        var result = await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 100, "main");

        // Assert
        Assert.True(result.Success);
        var bid = await _bidRepository.GetBidByUserAndItemAsync(_testRaider1Id, itemId);
        Assert.NotNull(bid);
        Assert.Equal(100, bid.Amount);
        Assert.Equal("main", bid.BidType);
    }

    [Fact]
    public async Task RetractBid_WhileAuctionIsOpen_Succeeds()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Place bids — raider2 outbids raider1 so raider1 is not the winner and can retract
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 50, "main");
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, itemId, 100, "main");

        // Act
        var result = await _auctionService.RetractBidAsync(_testRaider1Id, itemId);

        // Assert
        Assert.True(result.Success);
        var bid = await _bidRepository.GetBidByUserAndItemAsync(_testRaider1Id, itemId);
        Assert.Null(bid);
    }

    [Fact]
    public async Task PlaceBid_OnClosedAuction_IsRejected()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        await _auctionService.CloseAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Act
        var result = await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 50, "main");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("open auctions", result.ErrorMessage);
    }

    [Fact]
    public async Task GetAuctionDetail_WhenClosed_RevealsAllBidsSortedByPriorityAndAmount()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Place bids: 100 MAIN vs 150 ALT - MAIN should win due to type priority
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 100, "main");
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, itemId, 150, "alt");

        await _auctionService.CloseAuctionAsync(auctionId);

        // Act
        var sortedBids = await _auctionService.GetSortedBidsForItemAsync(itemId);

        // Assert
        Assert.Equal(2, sortedBids.Count);
        Assert.Equal("main", sortedBids[0].Bid.BidType); // MAIN wins even with lower amount
        Assert.Equal(100, sortedBids[0].Bid.Amount);
        Assert.Equal("alt", sortedBids[1].Bid.BidType);
        Assert.Equal(150, sortedBids[1].Bid.Amount);
    }

    [Fact]
    public async Task SortBids_WithSameAmount_AppliesBidTypePriority_MainBeforeAltBeforeGreed()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Create a third raider for this test
        var raider3Id = await CreateTestUser("raider3@test.com", "Raider3", "raider", 500);

        // Place bids with same amount but different types
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 100, "greed");
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, itemId, 100, "main");
        await _auctionService.PlaceOrUpdateBidAsync(raider3Id, itemId, 100, "alt");

        await _auctionService.CloseAuctionAsync(auctionId);

        // Act
        var sortedBids = await _auctionService.GetSortedBidsForItemAsync(itemId);

        // Assert
        Assert.Equal(3, sortedBids.Count);
        Assert.Equal("main", sortedBids[0].Bid.BidType);
        Assert.Equal("alt", sortedBids[1].Bid.BidType);
        Assert.Equal("greed", sortedBids[2].Bid.BidType);
    }

    [Fact]
    public async Task SortBids_BidTypePriority_OverridesAmount_MainBeatsHigherAltBid()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Place bids: 50 DKP ALT vs 10 DKP MAIN - MAIN should win
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 50, "alt");
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, itemId, 10, "main");

        await _auctionService.CloseAuctionAsync(auctionId);

        // Act
        var sortedBids = await _auctionService.GetSortedBidsForItemAsync(itemId);

        // Assert
        Assert.Equal(2, sortedBids.Count);
        Assert.Equal("main", sortedBids[0].Bid.BidType); // MAIN wins even with lower amount
        Assert.Equal(10, sortedBids[0].Bid.Amount);
        Assert.Equal("alt", sortedBids[1].Bid.BidType);
        Assert.Equal(50, sortedBids[1].Bid.Amount);
    }

    [Fact]
    public async Task ResolveTie_WithSameAmountAndBidType_UsesBidTimestamp()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Place bids with same amount and type (first bid should win)
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 100, "main");
        await Task.Delay(10); // Small delay to ensure different timestamps
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, itemId, 100, "main");

        await _auctionService.CloseAuctionAsync(auctionId);

        // Act
        var sortedBids = await _auctionService.GetSortedBidsForItemAsync(itemId);

        // Assert
        Assert.Equal(2, sortedBids.Count);
        Assert.Equal(_testRaider1Id, sortedBids[0].Bid.UserId); // First bidder wins
        Assert.Equal(_testRaider2Id, sortedBids[1].Bid.UserId);
        Assert.True(sortedBids[0].Bid.PlacedAt <= sortedBids[1].Bid.PlacedAt); // Earlier bid wins
    }

    [Fact]
    public async Task DeliverItem_DeductsDkpFromWinnerWithinTransaction()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = (await _auctionRepository.GetAuctionItemsAsync(auctionId)).ToList();
        var itemId = auctionItems[0].Id;

        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 100, "main");
        await _auctionService.CloseAuctionAsync(auctionId);

        var initialBalance = (await _userRepository.FindByIdAsync(_testRaider1Id))!.DkpBalance;

        // Act
        var result = await _auctionService.DeliverItemAsync(itemId, _testRaider1Id, 100, _testAdminId);

        // Assert
        Assert.True(result.Success);
        var item = await _auctionRepository.GetAuctionItemByIdAsync(itemId);
        Assert.NotNull(item);
        Assert.True(item.Delivered);
        Assert.Equal(_testRaider1Id, item.WinnerId);
        Assert.Equal(100, item.FinalPrice);

        var finalBalance = (await _userRepository.FindByIdAsync(_testRaider1Id))!.DkpBalance;
        Assert.Equal(initialBalance - 100, finalBalance);
    }

    [Fact]
    public async Task DeliverItem_WhenWinnerBalanceWouldGoNegative_IsBlockedWithDescriptiveError()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        await _auctionService.PlaceOrUpdateBidAsync(_testRaider2Id, itemId, 250, "main");
        await _auctionService.CloseAuctionAsync(auctionId);

        // Manually reduce raider's balance to simulate spending elsewhere
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("UPDATE users SET dkp_balance = 200 WHERE id = @Id", new { Id = _testRaider2Id });

        // Act - Try to deliver item for 250 DKP when raider only has 200
        var result = await _auctionService.DeliverItemAsync(itemId, _testRaider2Id, 250, _testAdminId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("balance", result.ErrorMessage.ToLower());
        Assert.Contains("200", result.ErrorMessage);
        Assert.Contains("250", result.ErrorMessage);
    }

    [Fact]
    public async Task CancelAuction_WhilePending_DiscardsBidsAndDeductsNoDkp()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;

        // Act
        var result = await _auctionService.CancelAuctionAsync(auctionId);

        // Assert
        Assert.True(result.Success);
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        Assert.NotNull(auction);
        Assert.Equal("cancelled", auction.Status);
    }

    [Fact]
    public async Task CancelAuction_WhileOpen_DiscardsBidsAndDeductsNoDkp()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        var auctionItems = await _auctionRepository.GetAuctionItemsAsync(auctionId);
        var itemId = auctionItems.First().Id;

        // Place a bid
        await _auctionService.PlaceOrUpdateBidAsync(_testRaider1Id, itemId, 100, "main");
        var initialBalance = (await _userRepository.FindByIdAsync(_testRaider1Id))!.DkpBalance;

        // Act
        var result = await _auctionService.CancelAuctionAsync(auctionId);

        // Assert
        Assert.True(result.Success);
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        Assert.NotNull(auction);
        Assert.Equal("cancelled", auction.Status);

        // Verify bid was discarded
        var bid = await _bidRepository.GetBidByUserAndItemAsync(_testRaider1Id, itemId);
        Assert.Null(bid);

        // Verify balance unchanged
        var finalBalance = (await _userRepository.FindByIdAsync(_testRaider1Id))!.DkpBalance;
        Assert.Equal(initialBalance, finalBalance);
    }

    [Fact]
    public async Task CancelAuction_WhileClosed_IsRejected()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        await _auctionService.CloseAuctionAsync(auctionId);

        // Act
        var result = await _auctionService.CancelAuctionAsync(auctionId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Closed auctions cannot be cancelled", result.ErrorMessage);
    }

    [Fact]
    public async Task AuctionHistory_IsVisibleToAllAuthenticatedUsers()
    {
        // Arrange
        var items = new List<(string Name, int MinimumBid, string? ImageUrl)> { ("Sword", 10, null) };
        var createResult = await _auctionService.CreateAuctionAsync(_testGuildId, "Test Auction", DateTime.UtcNow.AddMinutes(30), _testAdminId, items);
        var auctionId = createResult.Auction!.Id;
        await _auctionService.StartAuctionAsync(auctionId);
        await _auctionService.CloseAuctionAsync(auctionId);

        // Act
        var closedAuctions = await _auctionService.GetClosedAuctionsAsync(_testGuildId);

        // Assert
        Assert.Contains(closedAuctions, a => a.Id == auctionId);
    }
}
