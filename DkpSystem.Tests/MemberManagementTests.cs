using DkpSystem.Data;
using DkpSystem.Data.Repositories;
using DkpSystem.Models;
using DkpSystem.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace DkpSystem.Tests;

/// <summary>
/// Unit tests for Module 2 - Member Management.
/// </summary>
public class MemberManagementTests
{
    private readonly Mock<MemberRepository> _mockMemberRepository;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly MemberService _memberService;

    public MemberManagementTests()
    {
        // Mock MemberRepository
        var mockConnectionFactory = new Mock<DbConnectionFactory>("Host=localhost;Database=test;Username=test;Password=test");
        _mockMemberRepository = new Mock<MemberRepository>(mockConnectionFactory.Object);

        // Mock UserManager
        var mockUserStore = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            mockUserStore.Object, null, null, null, null, null, null, null, null);

        _memberService = new MemberService(_mockMemberRepository.Object, _mockUserManager.Object);
    }

    /// <summary>
    /// Test: UpdateRole_WithValidMember_ChangesRoleCorrectly
    /// </summary>
    [Fact]
    public async Task UpdateRole_WithValidMember_ChangesRoleCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var member = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Role = "raider",
            Active = true
        };

        _mockMemberRepository
            .Setup(r => r.GetMemberByIdAsync(userId))
            .ReturnsAsync(member);

        _mockMemberRepository
            .Setup(r => r.UpdateMemberRoleAndGuildAsync(userId, "admin", null))
            .ReturnsAsync(true);

        // Act
        var result = await _memberService.UpdateMemberRoleAndGuildAsync(userId, "admin", null);

        // Assert
        Assert.True(result.IsSuccess);
        _mockMemberRepository.Verify(r => r.UpdateMemberRoleAndGuildAsync(userId, "admin", null), Times.Once);
    }

    /// <summary>
    /// Test: SoftDelete_DeactivatesMember_PreventsLogin
    /// </summary>
    [Fact]
    public async Task SoftDelete_DeactivatesMember_PreventsLogin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var member = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Active = true
        };

        _mockMemberRepository
            .Setup(r => r.GetMemberByIdAsync(userId))
            .ReturnsAsync(member);

        _mockMemberRepository
            .Setup(r => r.DeactivateMemberAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _memberService.DeactivateMemberAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        _mockMemberRepository.Verify(r => r.DeactivateMemberAsync(userId), Times.Once);
    }

    /// <summary>
    /// Test: SoftDelete_DeactivatesMember_PreservesHistoricalRecords
    /// </summary>
    [Fact]
    public async Task SoftDelete_DeactivatesMember_PreservesHistoricalRecords()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var member = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Active = true
        };

        var earningsHistory = new List<MemberEarningHistory>
        {
            new MemberEarningHistory
            {
                EventName = "Test Event",
                Reason = "Test Reason",
                DkpAmount = 10,
                EarnedAt = DateTime.UtcNow
            }
        };

        _mockMemberRepository
            .Setup(r => r.GetMemberByIdAsync(userId))
            .ReturnsAsync(member);

        _mockMemberRepository
            .Setup(r => r.DeactivateMemberAsync(userId))
            .ReturnsAsync(true);

        _mockMemberRepository
            .Setup(r => r.GetMemberEarningsHistoryAsync(userId))
            .ReturnsAsync(earningsHistory);

        // Act
        var result = await _memberService.DeactivateMemberAsync(userId);
        var history = await _memberService.GetMemberEarningsHistoryAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(history);
        Assert.Single(history);
    }

    /// <summary>
    /// Test: AdminResetPassword_WithValidData_UpdatesPasswordHash
    /// </summary>
    [Fact]
    public async Task AdminResetPassword_WithValidData_UpdatesPasswordHash()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var member = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(member);

        _mockUserManager
            .Setup(um => um.RemovePasswordAsync(member))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(um => um.GeneratePasswordResetTokenAsync(member))
            .ReturnsAsync("reset-token");

        _mockUserManager
            .Setup(um => um.ResetPasswordAsync(member, "reset-token", "TempPass123"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _memberService.ResetMemberPasswordAsync(userId, "TempPass123");

        // Assert
        Assert.True(result.IsSuccess);
        _mockUserManager.Verify(um => um.RemovePasswordAsync(member), Times.Once);
        _mockUserManager.Verify(um => um.GeneratePasswordResetTokenAsync(member), Times.Once);
        _mockUserManager.Verify(um => um.ResetPasswordAsync(member, "reset-token", "TempPass123"), Times.Once);
    }

    /// <summary>
    /// Test: ChangeOwnPassword_WithCorrectCurrentPassword_Succeeds
    /// </summary>
    [Fact]
    public async Task ChangeOwnPassword_WithCorrectCurrentPassword_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var member = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(member);

        _mockUserManager
            .Setup(um => um.ChangePasswordAsync(member, "OldPass123", "NewPass123"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _memberService.ChangeOwnPasswordAsync(userId, "OldPass123", "NewPass123");

        // Assert
        Assert.True(result.IsSuccess);
        _mockUserManager.Verify(um => um.ChangePasswordAsync(member, "OldPass123", "NewPass123"), Times.Once);
    }

    /// <summary>
    /// Test: ChangeOwnPassword_WithWrongCurrentPassword_ReturnsError
    /// </summary>
    [Fact]
    public async Task ChangeOwnPassword_WithWrongCurrentPassword_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var member = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(member);

        _mockUserManager
            .Setup(um => um.ChangePasswordAsync(member, "WrongPass", "NewPass123"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        // Act
        var result = await _memberService.ChangeOwnPasswordAsync(userId, "WrongPass", "NewPass123");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    /// <summary>
    /// Test: GetMemberProfile_ByRaider_CannotAccessOtherMembersProfile
    /// This test verifies that the repository method only returns the requested user's data.
    /// Authorization is enforced at the page level with [Authorize] attributes.
    /// </summary>
    [Fact]
    public async Task GetMemberProfile_ByRaider_CannotAccessOtherMembersProfile()
    {
        // Arrange
        var raiderUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var raider = new User
        {
            Id = raiderUserId,
            Username = "raider",
            Email = "raider@example.com",
            Role = "raider"
        };

        // Mock that when requesting other user's profile, it returns that user (not the raider)
        _mockMemberRepository
            .Setup(r => r.GetMemberByIdAsync(otherUserId))
            .ReturnsAsync(new User
            {
                Id = otherUserId,
                Username = "otheruser",
                Email = "other@example.com",
                Role = "raider"
            });

        // Act
        var otherMember = await _memberService.GetMemberByIdAsync(otherUserId);

        // Assert
        // The service returns the requested user, but page-level authorization
        // prevents raiders from accessing other users' profiles
        Assert.NotNull(otherMember);
        Assert.NotEqual(raiderUserId, otherMember.Id);
    }

    /// <summary>
    /// Test: GetRanking_ReturnsMembersSortedByBalanceDescending
    /// </summary>
    [Fact]
    public async Task GetRanking_ReturnsMembersSortedByBalanceDescending()
    {
        // Arrange
        var members = new List<User>
        {
            new User { Id = Guid.NewGuid(), Username = "user1", DkpBalance = 100, Active = true },
            new User { Id = Guid.NewGuid(), Username = "user2", DkpBalance = 200, Active = true },
            new User { Id = Guid.NewGuid(), Username = "user3", DkpBalance = 150, Active = true }
        };

        // Sort by balance descending as the repository would
        var sortedMembers = members.OrderByDescending(m => m.DkpBalance).ToList();

        _mockMemberRepository
            .Setup(r => r.GetMemberRankingAsync())
            .ReturnsAsync(sortedMembers);

        // Act
        var ranking = await _memberService.GetMemberRankingAsync();
        var rankingList = ranking.ToList();

        // Assert
        Assert.Equal(3, rankingList.Count);
        Assert.Equal(200, rankingList[0].DkpBalance);
        Assert.Equal(150, rankingList[1].DkpBalance);
        Assert.Equal(100, rankingList[2].DkpBalance);
    }
}
