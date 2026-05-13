using DkpSystem.Data.Repositories;
using DkpSystem.Models;

namespace DkpSystem.Services;

/// <summary>
/// Service for member management business logic.
/// </summary>
public interface IMemberService
{
    /// <summary>
    /// Gets all members from the system.
    /// </summary>
    /// <returns>A list of all users.</returns>
    Task<IEnumerable<User>> GetAllMembersAsync();

    /// <summary>
    /// Gets all members sorted by DKP balance descending.
    /// </summary>
    /// <returns>A list of users sorted by DKP balance.</returns>
    Task<IEnumerable<User>> GetMemberRankingAsync();

    /// <summary>
    /// Gets all active members for a guild sorted by DKP balance descending.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of active guild members sorted by DKP balance.</returns>
    Task<IEnumerable<User>> GetMemberRankingByGuildAsync(Guid guildId);

    /// <summary>
    /// Gets a member by their ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetMemberByIdAsync(Guid userId);

    /// <summary>
    /// Updates a member's role and guild assignment.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="role">The new role (must be 'admin', 'officer', or 'raider').</param>
    /// <param name="guildId">The new guild ID (can be null).</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    Task<ServiceResult> UpdateMemberRoleAndGuildAsync(Guid userId, string role, Guid? guildId);

    /// <summary>
    /// Deactivates a member (soft delete). Historical records are preserved.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    Task<ServiceResult> DeactivateMemberAsync(Guid userId);

    /// <summary>
    /// Resets a member's password to a temporary password provided by the admin.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="temporaryPassword">The temporary password to set.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    Task<ServiceResult> ResetMemberPasswordAsync(Guid userId, string temporaryPassword);

    /// <summary>
    /// Changes a member's own password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    Task<ServiceResult> ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword);

    /// <summary>
    /// Gets the DKP earnings history for a specific member.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of DKP earnings with event details.</returns>
    Task<IEnumerable<MemberEarningHistory>> GetMemberEarningsHistoryAsync(Guid userId);

    /// <summary>
    /// Gets the won items history for a specific member (DKP spending).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of won auction items.</returns>
    Task<IEnumerable<MemberWonItemHistory>> GetMemberWonItemsHistoryAsync(Guid userId);

    /// <summary>
    /// Gets all guilds from the system.
    /// </summary>
    /// <returns>A list of all guilds.</returns>
    Task<IEnumerable<Guild>> GetAllGuildsAsync();
}
