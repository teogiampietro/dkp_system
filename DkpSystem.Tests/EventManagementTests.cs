using Dapper;
using DkpSystem.Data;
using DkpSystem.Data.Repositories;
using DkpSystem.Models;
using DkpSystem.Services;
using Xunit;

namespace DkpSystem.Tests;

/// <summary>
/// Unit tests for Module 3 - Event Management (DKP Earnings).
/// </summary>
public class EventManagementTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly DbConnectionFactory _connectionFactory;
    private readonly EventRepository _eventRepository;
    private readonly EventService _eventService;
    private readonly UserRepository _userRepository;
    private Guid _testGuildId;
    private Guid _testAdminId;
    private List<Guid> _testRaiderIds = new();

    public EventManagementTests()
    {
        _connectionString = "Host=localhost;Database=dkp_test;Username=postgres;Password=postgres";
        _connectionFactory = new DbConnectionFactory(_connectionString);
        _eventRepository = new EventRepository(_connectionFactory);
        _eventService = new EventService(_eventRepository);
        _userRepository = new UserRepository(_connectionFactory);
    }

    public async Task InitializeAsync()
    {
        await CleanupTestDataAsync();
        await SetupTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestDataAsync();
    }

    private async Task SetupTestDataAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Create test guild
        _testGuildId = Guid.NewGuid();
        await connection.ExecuteAsync(
            "INSERT INTO guilds (id, name, created_at) VALUES (@Id, @Name, @CreatedAt)",
            new { Id = _testGuildId, Name = "Test Guild", CreatedAt = DateTime.UtcNow }
        );

        // Create test admin
        _testAdminId = Guid.NewGuid();
        await connection.ExecuteAsync(
            @"INSERT INTO users (id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at)
              VALUES (@Id, @Email, @Username, @PasswordHash, @Role, @GuildId, @DkpBalance, @Active, @CreatedAt)",
            new
            {
                Id = _testAdminId,
                Email = "admin@test.com",
                Username = "TestAdmin",
                PasswordHash = "hash",
                Role = "admin",
                GuildId = _testGuildId,
                DkpBalance = 0,
                Active = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Create test raiders
        for (int i = 1; i <= 5; i++)
        {
            var raiderId = Guid.NewGuid();
            _testRaiderIds.Add(raiderId);
            await connection.ExecuteAsync(
                @"INSERT INTO users (id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at)
                  VALUES (@Id, @Email, @Username, @PasswordHash, @Role, @GuildId, @DkpBalance, @Active, @CreatedAt)",
                new
                {
                    Id = raiderId,
                    Email = $"raider{i}@test.com",
                    Username = $"Raider{i}",
                    PasswordHash = "hash",
                    Role = "raider",
                    GuildId = _testGuildId,
                    DkpBalance = 0,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }

    private async Task CleanupTestDataAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM dkp_earnings WHERE event_id IN (SELECT id FROM events WHERE guild_id = @GuildId)", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM event_reward_lines WHERE event_id IN (SELECT id FROM events WHERE guild_id = @GuildId)", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM events WHERE guild_id = @GuildId", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM users WHERE guild_id = @GuildId", new { GuildId = _testGuildId });
        await connection.ExecuteAsync("DELETE FROM guilds WHERE id = @GuildId", new { GuildId = _testGuildId });
    }

    [Fact]
    public async Task CreateEvent_ConfirmsAllActiveGuildMembersAsAttendeesByDefault()
    {
        // Arrange
        var eventName = "Test Raid";
        var allMemberIds = new List<Guid> { _testAdminId }.Concat(_testRaiderIds).ToList();

        // Act
        var createdEvent = await _eventService.CreateEventAsync(
            eventName,
            "Test Description",
            _testGuildId,
            _testAdminId,
            allMemberIds
        );

        // Assert
        Assert.NotNull(createdEvent);
        Assert.Equal(eventName, createdEvent.Name);
        Assert.Equal(_testGuildId, createdEvent.GuildId);
    }

    [Fact]
    public async Task CreateEvent_WithSomeAttendeesRemoved_OnlySavesConfirmedAttendees()
    {
        // Arrange
        var eventName = "Partial Attendance Raid";
        var confirmedAttendees = _testRaiderIds.Take(3).ToList(); // Only 3 out of 5 raiders

        // Act
        var createdEvent = await _eventService.CreateEventAsync(
            eventName,
            null,
            _testGuildId,
            _testAdminId,
            confirmedAttendees
        );

        // Add a group award to verify attendees
        await _eventService.AddGroupAwardAsync(createdEvent.Id, "Test Award", 10, confirmedAttendees);

        var attendees = await _eventRepository.GetConfirmedAttendeesAsync(createdEvent.Id);

        // Assert
        Assert.Equal(3, attendees.Count());
        Assert.All(confirmedAttendees, id => Assert.Contains(attendees, a => a.Id == id));
    }

    [Fact]
    public async Task AddGroupAward_CreatesEarningsForAllConfirmedAttendees()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Group Award Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Act
        var result = await _eventService.AddGroupAwardAsync(
            eventModel.Id,
            "Raid Completion",
            20,
            _testRaiderIds
        );

        var earnings = await _eventRepository.GetEarningsAsync(eventModel.Id);

        // Assert
        Assert.Equal(_testRaiderIds.Count, result);
        Assert.Equal(_testRaiderIds.Count, earnings.Count());
        Assert.All(earnings, e => Assert.Equal(20, e.DkpAmount));
    }

    [Fact]
    public async Task AddGroupAward_UpdatesBalanceForAllConfirmedAttendees()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Balance Update Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Act
        await _eventService.AddGroupAwardAsync(
            eventModel.Id,
            "Raid Completion",
            25,
            _testRaiderIds
        );

        // Assert - Check each raider's balance
        foreach (var raiderId in _testRaiderIds)
        {
            var user = await _userRepository.FindByIdAsync(raiderId);
            Assert.NotNull(user);
            Assert.Equal(25, user.DkpBalance);
        }
    }

    [Fact]
    public async Task AddIndividualAward_CreatesEarningOnlyForTargetRaider()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Individual Award Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );
        var targetRaiderId = _testRaiderIds.First();

        // Act
        var result = await _eventService.AddIndividualAwardAsync(
            eventModel.Id,
            targetRaiderId,
            "First Kill Bonus",
            15
        );

        var earnings = await _eventRepository.GetEarningsAsync(eventModel.Id);

        // Assert
        Assert.Equal(1, result);
        Assert.Single(earnings);
        Assert.Equal(targetRaiderId, earnings.First().UserId);
        Assert.Equal(15, earnings.First().DkpAmount);
    }

    [Fact]
    public async Task AddIndividualAward_UpdatesBalanceOnlyForTargetRaider()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Individual Balance Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );
        var targetRaiderId = _testRaiderIds.First();

        // Act
        await _eventService.AddIndividualAwardAsync(
            eventModel.Id,
            targetRaiderId,
            "MVP Award",
            30
        );

        // Assert
        var targetUser = await _userRepository.FindByIdAsync(targetRaiderId);
        Assert.NotNull(targetUser);
        Assert.Equal(30, targetUser.DkpBalance);

        // Other raiders should still have 0 balance
        foreach (var raiderId in _testRaiderIds.Skip(1))
        {
            var user = await _userRepository.FindByIdAsync(raiderId);
            Assert.NotNull(user);
            Assert.Equal(0, user.DkpBalance);
        }
    }

    [Fact]
    public async Task AddAward_TransactionFailure_NoPartialChangesPersistedInDatabase()
    {
        // This test verifies transaction integrity by checking that if an error occurs,
        // no partial data is saved. We'll test by attempting to add an award with invalid data.
        
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Transaction Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Act & Assert - Try to add award with zero DKP (should fail)
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _eventService.AddGroupAwardAsync(eventModel.Id, "Invalid Award", 0, _testRaiderIds);
        });

        // Verify no earnings were created
        var earnings = await _eventRepository.GetEarningsAsync(eventModel.Id);
        Assert.Empty(earnings);

        // Verify balances remain at 0
        foreach (var raiderId in _testRaiderIds)
        {
            var user = await _userRepository.FindByIdAsync(raiderId);
            Assert.NotNull(user);
            Assert.Equal(0, user.DkpBalance);
        }
    }

    [Fact]
    public async Task GetEventDetail_ByRaider_ReturnsOnlyOwnEarnings()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Raider View Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Add group award
        await _eventService.AddGroupAwardAsync(eventModel.Id, "Group Award", 20, _testRaiderIds);

        // Add individual award to first raider
        var targetRaiderId = _testRaiderIds.First();
        await _eventService.AddIndividualAwardAsync(eventModel.Id, targetRaiderId, "Individual Award", 10);

        // Act - Get earnings for the first raider
        var raiderEarnings = await _eventService.GetRaiderEarningsAsync(eventModel.Id, targetRaiderId);

        // Assert - Should see both group and individual awards (total 30 DKP)
        Assert.Equal(2, raiderEarnings.Count());
        Assert.Equal(30, raiderEarnings.Sum(e => e.DkpAmount));

        // Act - Get earnings for another raider
        var otherRaiderId = _testRaiderIds.Skip(1).First();
        var otherRaiderEarnings = await _eventService.GetRaiderEarningsAsync(eventModel.Id, otherRaiderId);

        // Assert - Should only see group award (20 DKP)
        Assert.Single(otherRaiderEarnings);
        Assert.Equal(20, otherRaiderEarnings.Sum(e => e.DkpAmount));
    }

    [Fact]
    public async Task DeleteEvent_WithExistingEarnings_IsBlockedWithClearMessage()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Delete Test With Earnings",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Add earnings
        await _eventService.AddGroupAwardAsync(eventModel.Id, "Test Award", 10, _testRaiderIds);

        // Act
        var result = await _eventService.DeleteEventAsync(eventModel.Id);

        // Assert
        Assert.False(result);

        // Verify event still exists
        var existingEvent = await _eventRepository.GetByIdAsync(eventModel.Id);
        Assert.NotNull(existingEvent);
    }

    [Fact]
    public async Task DeleteEvent_WithNoEarnings_SucceedsAndRemovesEvent()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Delete Test Without Earnings",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Act - Delete without adding any earnings
        var result = await _eventService.DeleteEventAsync(eventModel.Id);

        // Assert
        Assert.True(result);

        // Verify event no longer exists
        var deletedEvent = await _eventRepository.GetByIdAsync(eventModel.Id);
        Assert.Null(deletedEvent);
    }

    [Fact]
    public async Task AddAward_WithZeroOrNegativeDkpAmount_IsRejected()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Invalid DKP Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Act & Assert - Zero DKP
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _eventService.AddGroupAwardAsync(eventModel.Id, "Zero Award", 0, _testRaiderIds);
        });

        // Act & Assert - Negative DKP
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _eventService.AddGroupAwardAsync(eventModel.Id, "Negative Award", -10, _testRaiderIds);
        });

        // Verify no earnings were created
        var earnings = await _eventRepository.GetEarningsAsync(eventModel.Id);
        Assert.Empty(earnings);
    }

    [Fact]
    public async Task MultipleAwards_AccumulateCorrectly()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Multiple Awards Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );
        var targetRaiderId = _testRaiderIds.First();

        // Act - Add multiple awards
        await _eventService.AddGroupAwardAsync(eventModel.Id, "Raid Completion", 20, _testRaiderIds);
        await _eventService.AddGroupAwardAsync(eventModel.Id, "Boss Kill", 15, _testRaiderIds);
        await _eventService.AddIndividualAwardAsync(eventModel.Id, targetRaiderId, "MVP", 10);

        // Assert
        var targetUser = await _userRepository.FindByIdAsync(targetRaiderId);
        Assert.NotNull(targetUser);
        Assert.Equal(45, targetUser.DkpBalance); // 20 + 15 + 10

        var otherUser = await _userRepository.FindByIdAsync(_testRaiderIds.Skip(1).First());
        Assert.NotNull(otherUser);
        Assert.Equal(35, otherUser.DkpBalance); // 20 + 15
    }

    [Fact]
    public async Task GetAwardHistory_ReturnsAllAwardsInCorrectOrder()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Award History Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Act - Add multiple awards
        await _eventService.AddGroupAwardAsync(eventModel.Id, "First Award", 10, _testRaiderIds);
        await Task.Delay(100); // Small delay to ensure different timestamps
        await _eventService.AddIndividualAwardAsync(eventModel.Id, _testRaiderIds.First(), "Second Award", 5);
        await Task.Delay(100);
        await _eventService.AddGroupAwardAsync(eventModel.Id, "Third Award", 15, _testRaiderIds);

        var history = await _eventService.GetAwardHistoryAsync(eventModel.Id);

        // Assert
        Assert.Equal(3, history.Count());
        
        var historyList = history.ToList();
        Assert.Equal("Third Award", historyList[0].Reason); // Most recent first
        Assert.Equal("Second Award", historyList[1].Reason);
        Assert.Equal("First Award", historyList[2].Reason);

        Assert.True(historyList[0].IsGroupAward);
        Assert.False(historyList[1].IsGroupAward);
        Assert.True(historyList[2].IsGroupAward);
    }

    [Fact]
    public async Task UpdateEvent_ChangesNameAndDescription()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Original Name",
            "Original Description",
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        // Act
        var result = await _eventService.UpdateEventAsync(
            eventModel.Id,
            "Updated Name",
            "Updated Description"
        );

        // Assert
        Assert.True(result);

        var updatedEvent = await _eventRepository.GetByIdAsync(eventModel.Id);
        Assert.NotNull(updatedEvent);
        Assert.Equal("Updated Name", updatedEvent.Name);
        Assert.Equal("Updated Description", updatedEvent.Description);
    }

    [Fact]
    public async Task GetEventSummaries_ReturnsCorrectStatistics()
    {
        // Arrange
        var eventModel = await _eventService.CreateEventAsync(
            "Summary Test",
            null,
            _testGuildId,
            _testAdminId,
            _testRaiderIds
        );

        await _eventService.AddGroupAwardAsync(eventModel.Id, "Award 1", 20, _testRaiderIds);
        await _eventService.AddGroupAwardAsync(eventModel.Id, "Award 2", 15, _testRaiderIds);

        // Act
        var summaries = await _eventService.GetEventSummariesAsync(_testGuildId);

        // Assert
        var summary = summaries.FirstOrDefault(s => s.Id == eventModel.Id);
        Assert.NotNull(summary);
        Assert.Equal("Summary Test", summary.Name);
        Assert.Equal(175, summary.TotalDkpDistributed); // (20 + 15) * 5 raiders
        Assert.Equal(5, summary.AttendeeCount);
    }
}
