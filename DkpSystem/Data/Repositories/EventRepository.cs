using System.Data;
using Dapper;
using DkpSystem.Models;

namespace DkpSystem.Data.Repositories;

/// <summary>
/// Repository for managing events and their attendees.
/// </summary>
public class EventRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public EventRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="eventModel">The event to create.</param>
    /// <returns>The created event with generated ID.</returns>
    public async Task<Event> CreateAsync(Event eventModel)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO events (id, guild_id, name, description, created_by, created_at)
            VALUES (@Id, @GuildId, @Name, @Description, @CreatedBy, @CreatedAt)
            RETURNING id, guild_id, name, description, created_by, created_at";

        eventModel.Id = Guid.NewGuid();
        eventModel.CreatedAt = DateTime.UtcNow;

        var result = await connection.QuerySingleAsync<Event>(sql, eventModel);
        return result;
    }

    /// <summary>
    /// Gets an event by its ID.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    public async Task<Event?> GetByIdAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, guild_id, name, description, created_by, created_at
            FROM events
            WHERE id = @EventId";

        return await connection.QuerySingleOrDefaultAsync<Event>(sql, new { EventId = eventId });
    }

    /// <summary>
    /// Gets all events for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of events ordered by creation date descending.</returns>
    public async Task<IEnumerable<Event>> GetByGuildIdAsync(Guid guildId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, guild_id, name, description, created_by, created_at
            FROM events
            WHERE guild_id = @GuildId
            ORDER BY created_at DESC";

        return await connection.QueryAsync<Event>(sql, new { GuildId = guildId });
    }

    /// <summary>
    /// Updates an event's name and description.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    public async Task<bool> UpdateAsync(Guid eventId, string name, string? description)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE events
            SET name = @Name, description = @Description
            WHERE id = @EventId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { EventId = eventId, Name = name, Description = description });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Deletes an event if it has no associated earnings.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>True if the event was deleted; false if it has earnings.</returns>
    public async Task<bool> DeleteAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        // Check if event has earnings
        const string checkSql = @"
            SELECT COUNT(*) FROM dkp_earnings WHERE event_id = @EventId";
        
        var earningsCount = await connection.ExecuteScalarAsync<int>(checkSql, new { EventId = eventId });
        
        if (earningsCount > 0)
        {
            return false;
        }

        const string deleteSql = @"DELETE FROM events WHERE id = @EventId";
        var rowsAffected = await connection.ExecuteAsync(deleteSql, new { EventId = eventId });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Saves the list of confirmed attendees for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="attendeeIds">The list of user IDs who attended.</param>
    public async Task SaveAttendeesAsync(Guid eventId, IEnumerable<Guid> attendeeIds)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        // Store attendees in a temporary table or use a JSON column
        // For now, we'll track attendees through the reward lines they receive
        // This is handled in the EventService when awards are created
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets all active guild members for attendance selection.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of active users in the guild.</returns>
    public async Task<IEnumerable<User>> GetActiveGuildMembersAsync(Guid guildId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at
            FROM users
            WHERE guild_id = @GuildId AND active = true
            ORDER BY username";

        return await connection.QueryAsync<User>(sql, new { GuildId = guildId });
    }

    /// <summary>
    /// Gets all active members regardless of guild assignment.
    /// </summary>
    /// <returns>A list of all active users.</returns>
    public async Task<IEnumerable<User>> GetAllActiveMembersAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at
            FROM users
            WHERE active = true
            ORDER BY username";

        return await connection.QueryAsync<User>(sql);
    }

    /// <summary>
    /// Gets the total DKP distributed for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The total DKP amount distributed.</returns>
    public async Task<int> GetTotalDkpDistributedAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT COALESCE(SUM(dkp_amount), 0)
            FROM dkp_earnings
            WHERE event_id = @EventId";

        return await connection.ExecuteScalarAsync<int>(sql, new { EventId = eventId });
    }

    /// <summary>
    /// Gets the count of confirmed attendees for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The number of unique attendees.</returns>
    public async Task<int> GetAttendeeCountAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT COUNT(DISTINCT user_id)
            FROM dkp_earnings
            WHERE event_id = @EventId";

        return await connection.ExecuteScalarAsync<int>(sql, new { EventId = eventId });
    }

    /// <summary>
    /// Gets all reward lines for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of reward lines.</returns>
    public async Task<IEnumerable<EventRewardLine>> GetRewardLinesAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, event_id, reason, dkp_amount
            FROM event_reward_lines
            WHERE event_id = @EventId
            ORDER BY id";

        return await connection.QueryAsync<EventRewardLine>(sql, new { EventId = eventId });
    }

    /// <summary>
    /// Gets all earnings for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of DKP earnings.</returns>
    public async Task<IEnumerable<DkpEarning>> GetEarningsAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, user_id, event_id, reward_line_id, dkp_amount, earned_at
            FROM dkp_earnings
            WHERE event_id = @EventId
            ORDER BY earned_at DESC";

        return await connection.QueryAsync<DkpEarning>(sql, new { EventId = eventId });
    }

    /// <summary>
    /// Gets earnings for a specific user in an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of DKP earnings for the user.</returns>
    public async Task<IEnumerable<DkpEarning>> GetUserEarningsAsync(Guid eventId, Guid userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, user_id, event_id, reward_line_id, dkp_amount, earned_at
            FROM dkp_earnings
            WHERE event_id = @EventId AND user_id = @UserId
            ORDER BY earned_at DESC";

        return await connection.QueryAsync<DkpEarning>(sql, new { EventId = eventId, UserId = userId });
    }

    /// <summary>
    /// Creates a reward line for an event.
    /// </summary>
    /// <param name="rewardLine">The reward line to create.</param>
    /// <returns>The created reward line with generated ID.</returns>
    public async Task<EventRewardLine> CreateRewardLineAsync(EventRewardLine rewardLine)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO event_reward_lines (id, event_id, reason, dkp_amount)
            VALUES (@Id, @EventId, @Reason, @DkpAmount)
            RETURNING id, event_id, reason, dkp_amount";

        rewardLine.Id = Guid.NewGuid();

        var result = await connection.QuerySingleAsync<EventRewardLine>(sql, rewardLine);
        return result;
    }

    /// <summary>
    /// Creates DKP earnings and updates user balances within a transaction.
    /// </summary>
    /// <param name="earnings">The list of earnings to create.</param>
    /// <returns>The number of earnings created.</returns>
    public async Task<int> CreateEarningsAsync(IEnumerable<DkpEarning> earnings)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        using var transaction = connection.BeginTransaction();
        
        try
        {
            const string insertSql = @"
                INSERT INTO dkp_earnings (id, user_id, event_id, reward_line_id, dkp_amount, earned_at)
                VALUES (@Id, @UserId, @EventId, @RewardLineId, @DkpAmount, @EarnedAt)";

            const string updateBalanceSql = @"
                UPDATE users
                SET dkp_balance = dkp_balance + @DkpAmount
                WHERE id = @UserId";

            var earningsList = earnings.ToList();
            
            foreach (var earning in earningsList)
            {
                earning.Id = Guid.NewGuid();
                earning.EarnedAt = DateTime.UtcNow;
                
                await connection.ExecuteAsync(insertSql, earning, transaction);
                await connection.ExecuteAsync(updateBalanceSql, new { earning.UserId, earning.DkpAmount }, transaction);
            }

            transaction.Commit();
            return earningsList.Count;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Gets confirmed attendees for an event (users who have earnings).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of users who attended the event.</returns>
    public async Task<IEnumerable<User>> GetConfirmedAttendeesAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT DISTINCT u.id, u.email, u.username, u.password_hash, u.role, u.guild_id, u.dkp_balance, u.active, u.created_at
            FROM users u
            INNER JOIN dkp_earnings de ON u.id = de.user_id
            WHERE de.event_id = @EventId
            ORDER BY u.username";

        return await connection.QueryAsync<User>(sql, new { EventId = eventId });
    }
}
