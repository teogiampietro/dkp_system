using Dapper;
using DkpSystem.Models;
using System.Data;

namespace DkpSystem.Data.Repositories;

/// <summary>
/// Repository for member-related database operations.
/// </summary>
public class MemberRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public MemberRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gets all members from the database.
    /// </summary>
    /// <returns>A list of all users.</returns>
    public virtual async Task<IEnumerable<User>> GetAllMembersAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at
            FROM users
            ORDER BY username";
        
        return await connection.QueryAsync<User>(sql);
    }

    /// <summary>
    /// Gets all members sorted by DKP balance descending.
    /// </summary>
    /// <returns>A list of users sorted by DKP balance.</returns>
    public virtual async Task<IEnumerable<User>> GetMemberRankingAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at
            FROM users
            WHERE active = true
            ORDER BY dkp_balance DESC, username";
        
        return await connection.QueryAsync<User>(sql);
    }

    /// <summary>
    /// Gets all active members for a guild sorted by DKP balance descending.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of active guild members sorted by DKP balance.</returns>
    public virtual async Task<IEnumerable<User>> GetMemberRankingByGuildAsync(Guid guildId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at
            FROM users
            WHERE active = true AND guild_id = @GuildId
            ORDER BY dkp_balance DESC, username";

        return await connection.QueryAsync<User>(sql, new { GuildId = guildId });
    }

    /// <summary>
    /// Gets a member by their ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The user if found, null otherwise.</returns>
    public virtual async Task<User?> GetMemberByIdAsync(Guid userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at
            FROM users
            WHERE id = @UserId";
        
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Updates a member's role and guild assignment.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="role">The new role.</param>
    /// <param name="guildId">The new guild ID (can be null).</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public virtual async Task<bool> UpdateMemberRoleAndGuildAsync(Guid userId, string role, Guid? guildId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            UPDATE users
            SET role = @Role, guild_id = @GuildId
            WHERE id = @UserId";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId, Role = role, GuildId = guildId });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Soft deletes a member by setting their active status to false.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the deactivation was successful, false otherwise.</returns>
    public virtual async Task<bool> DeactivateMemberAsync(Guid userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            UPDATE users
            SET active = false
            WHERE id = @UserId";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Gets the DKP earnings history for a specific member.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of DKP earnings with event details.</returns>
    public virtual async Task<IEnumerable<MemberEarningHistory>> GetMemberEarningsHistoryAsync(Guid userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT
                e.name AS EventName,
                erl.reason AS Reason,
                de.dkp_amount AS DkpAmount,
                de.earned_at AS EarnedAt
            FROM dkp_earnings de
            INNER JOIN events e ON de.event_id = e.id
            INNER JOIN event_reward_lines erl ON de.reward_line_id = erl.id
            WHERE de.user_id = @UserId
            ORDER BY de.earned_at DESC";
        
        return await connection.QueryAsync<MemberEarningHistory>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Gets the won items history for a specific member (DKP spendings).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of won auction items.</returns>
    public virtual async Task<IEnumerable<MemberWonItemHistory>> GetMemberWonItemsHistoryAsync(Guid userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT
                a.name AS AuctionName,
                ai.name AS ItemName,
                ai.final_price AS DkpPaid,
                ai.delivered_at AS WonAt
            FROM auction_items ai
            INNER JOIN auctions a ON ai.auction_id = a.id
            WHERE ai.winner_id = @UserId
            AND ai.delivered = true
            ORDER BY ai.delivered_at DESC";
        
        return await connection.QueryAsync<MemberWonItemHistory>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Gets all guilds from the database.
    /// </summary>
    /// <returns>A list of all guilds.</returns>
    public virtual async Task<IEnumerable<Guild>> GetAllGuildsAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, name, created_at
            FROM guilds
            ORDER BY name";
        
        return await connection.QueryAsync<Guild>(sql);
    }
}

/// <summary>
/// Represents a member's DKP earning history entry.
/// </summary>
public class MemberEarningHistory
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the DKP award.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount earned.
    /// </summary>
    public int DkpAmount { get; set; }

    /// <summary>
    /// Gets or sets the date when the DKP was earned.
    /// </summary>
    public DateTime EarnedAt { get; set; }
}

/// <summary>
/// Represents a member's won item history entry.
/// </summary>
public class MemberWonItemHistory
{
    /// <summary>
    /// Gets or sets the auction name.
    /// </summary>
    public string AuctionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount paid for the item.
    /// </summary>
    public int DkpPaid { get; set; }

    /// <summary>
    /// Gets or sets the date when the item was won.
    /// </summary>
    public DateTime WonAt { get; set; }
}
