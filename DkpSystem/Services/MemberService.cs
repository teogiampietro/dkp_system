using DkpSystem.Data.Repositories;
using DkpSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace DkpSystem.Services;

/// <summary>
/// Service for member management business logic.
/// </summary>
public class MemberService
{
    private readonly MemberRepository _memberRepository;
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberService"/> class.
    /// </summary>
    /// <param name="memberRepository">The member repository.</param>
    /// <param name="userManager">The user manager.</param>
    public MemberService(MemberRepository memberRepository, UserManager<User> userManager)
    {
        _memberRepository = memberRepository;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets all members from the system.
    /// </summary>
    /// <returns>A list of all users.</returns>
    public async Task<IEnumerable<User>> GetAllMembersAsync()
    {
        return await _memberRepository.GetAllMembersAsync();
    }

    /// <summary>
    /// Gets all members sorted by DKP balance descending.
    /// </summary>
    /// <returns>A list of users sorted by DKP balance.</returns>
    public async Task<IEnumerable<User>> GetMemberRankingAsync()
    {
        return await _memberRepository.GetMemberRankingAsync();
    }

    /// <summary>
    /// Gets a member by their ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The user if found, null otherwise.</returns>
    public async Task<User?> GetMemberByIdAsync(Guid userId)
    {
        return await _memberRepository.GetMemberByIdAsync(userId);
    }

    /// <summary>
    /// Updates a member's role and guild assignment.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="role">The new role (must be 'admin' or 'raider').</param>
    /// <param name="guildId">The new guild ID (can be null).</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    public async Task<ServiceResult> UpdateMemberRoleAndGuildAsync(Guid userId, string role, Guid? guildId)
    {
        // Validate role
        if (role != "admin" && role != "raider")
        {
            return ServiceResult.Failure("Role must be either 'admin' or 'raider'.");
        }

        var member = await _memberRepository.GetMemberByIdAsync(userId);
        if (member == null)
        {
            return ServiceResult.Failure("Member not found.");
        }

        var success = await _memberRepository.UpdateMemberRoleAndGuildAsync(userId, role, guildId);
        if (!success)
        {
            return ServiceResult.Failure("Failed to update member role and guild.");
        }

        return ServiceResult.Success();
    }

    /// <summary>
    /// Deactivates a member (soft delete). Historical records are preserved.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    public async Task<ServiceResult> DeactivateMemberAsync(Guid userId)
    {
        var member = await _memberRepository.GetMemberByIdAsync(userId);
        if (member == null)
        {
            return ServiceResult.Failure("Member not found.");
        }

        if (!member.Active)
        {
            return ServiceResult.Failure("Member is already deactivated.");
        }

        var success = await _memberRepository.DeactivateMemberAsync(userId);
        if (!success)
        {
            return ServiceResult.Failure("Failed to deactivate member.");
        }

        return ServiceResult.Success();
    }

    /// <summary>
    /// Resets a member's password to a temporary password provided by the admin.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="temporaryPassword">The temporary password to set.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    public async Task<ServiceResult> ResetMemberPasswordAsync(Guid userId, string temporaryPassword)
    {
        if (string.IsNullOrWhiteSpace(temporaryPassword))
        {
            return ServiceResult.Failure("Temporary password cannot be empty.");
        }

        if (temporaryPassword.Length < 6)
        {
            return ServiceResult.Failure("Temporary password must be at least 6 characters long.");
        }

        var member = await _userManager.FindByIdAsync(userId.ToString());
        if (member == null)
        {
            return ServiceResult.Failure("Member not found.");
        }
        
        // Remove existing password
        var removeResult = await _userManager.RemovePasswordAsync(member);
        if (!removeResult.Succeeded)
        {
            return ServiceResult.Failure("Failed to remove existing password.");
        }

        var tokenResult = await _userManager.GeneratePasswordResetTokenAsync(member);
        if (string.IsNullOrWhiteSpace(tokenResult))
        {
            return ServiceResult.Failure($"Failed to generate token for reset password.");
        }
        
        var resetResult = await _userManager.ResetPasswordAsync(member, tokenResult, temporaryPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return ServiceResult.Failure($"Failed to set new password: {errors}");
        }

        return ServiceResult.Success();
    }

    /// <summary>
    /// Changes a member's own password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    public async Task<ServiceResult> ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            return ServiceResult.Failure("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return ServiceResult.Failure("New password is required.");
        }

        if (newPassword.Length < 6)
        {
            return ServiceResult.Failure("New password must be at least 6 characters long.");
        }

        var member = await _userManager.FindByIdAsync(userId.ToString());
        if (member == null)
        {
            return ServiceResult.Failure("Member not found.");
        }

        var result = await _userManager.ChangePasswordAsync(member, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return ServiceResult.Failure("Current password is incorrect or new password does not meet requirements.");
        }

        return ServiceResult.Success();
    }

    /// <summary>
    /// Gets the DKP earnings history for a specific member.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of DKP earnings with event details.</returns>
    public async Task<IEnumerable<MemberEarningHistory>> GetMemberEarningsHistoryAsync(Guid userId)
    {
        return await _memberRepository.GetMemberEarningsHistoryAsync(userId);
    }

    /// <summary>
    /// Gets the won items history for a specific member (DKP spendings).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of won auction items.</returns>
    public async Task<IEnumerable<MemberWonItemHistory>> GetMemberWonItemsHistoryAsync(Guid userId)
    {
        return await _memberRepository.GetMemberWonItemsHistoryAsync(userId);
    }

    /// <summary>
    /// Gets all guilds from the system.
    /// </summary>
    /// <returns>A list of all guilds.</returns>
    public async Task<IEnumerable<Guild>> GetAllGuildsAsync()
    {
        return await _memberRepository.GetAllGuildsAsync();
    }
}

/// <summary>
/// Represents the result of a service operation.
/// </summary>
public class ServiceResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private ServiceResult(bool isSuccess, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful service result.</returns>
    public static ServiceResult Success() => new ServiceResult(true);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed service result.</returns>
    public static ServiceResult Failure(string errorMessage) => new ServiceResult(false, errorMessage);
}
