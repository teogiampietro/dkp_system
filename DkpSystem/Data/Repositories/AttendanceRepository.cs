using Dapper;
using DkpSystem.Models;

namespace DkpSystem.Data.Repositories;

/// <summary>
/// Repository for attendance data, derived from <c>dkp_earnings</c>:
/// a user attended an event if they have at least one earning row for it.
/// </summary>
public class AttendanceRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttendanceRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public AttendanceRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gets the most recent events for a guild, optionally filtered by date range,
    /// ordered from newest to oldest.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="from">Optional inclusive lower bound on <c>created_at</c>.</param>
    /// <param name="to">Optional inclusive upper bound on <c>created_at</c>.</param>
    /// <param name="limit">Maximum number of events to return.</param>
    public async Task<IEnumerable<Event>> GetRecentEventsAsync(Guid guildId, DateTime? from, DateTime? to, int limit)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        const string sql = @"
            SELECT id, guild_id, name, description, created_by, created_at
            FROM events
            WHERE guild_id = @GuildId
              AND (@From::timestamptz IS NULL OR created_at >= @From::timestamptz)
              AND (@ToExclusive::timestamptz IS NULL OR created_at < @ToExclusive::timestamptz)
            ORDER BY created_at DESC
            LIMIT @Limit";

        var toExclusive = to?.Date.AddDays(1);

        return await connection.QueryAsync<Event>(sql, new
        {
            GuildId = guildId,
            From = from,
            To = to,
            ToExclusive = toExclusive,
            Limit = limit
        });
    }

    /// <summary>
    /// Gets all attendance pairs (user_id, event_id) for the given event IDs.
    /// A user is considered to have attended an event if they appear in
    /// <c>event_attendees</c> (pre-confirmed at event creation) or have at
    /// least one row in <c>dkp_earnings</c> for that event.
    /// </summary>
    public async Task<IEnumerable<(Guid UserId, Guid EventId)>> GetAttendanceAsync(IEnumerable<Guid> eventIds)
    {
        var ids = eventIds.ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();

        const string sql = @"
            SELECT user_id AS UserId, event_id AS EventId
            FROM event_attendees
            WHERE event_id = ANY(@EventIds)
            UNION
            SELECT DISTINCT user_id AS UserId, event_id AS EventId
            FROM dkp_earnings
            WHERE event_id = ANY(@EventIds)";

        var rows = await connection.QueryAsync<(Guid UserId, Guid EventId)>(sql, new { EventIds = ids });
        return rows;
    }
}
