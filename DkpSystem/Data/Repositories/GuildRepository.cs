using Dapper;
using DkpSystem.Models;

namespace DkpSystem.Data.Repositories;

/// <summary>
/// Repository for guild-related database operations.
/// </summary>
public class GuildRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public GuildRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Finds a guild by its invitation code.
    /// </summary>
    /// <param name="invitationCode">The invitation code to search for.</param>
    /// <returns>The guild if found, null otherwise.</returns>
    public async Task<Guild?> FindByInvitationCodeAsync(string invitationCode)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, name, invitation_code, created_at
            FROM guilds
            WHERE invitation_code = @InvitationCode";
        
        return await connection.QuerySingleOrDefaultAsync<Guild>(sql, new { InvitationCode = invitationCode });
    }

    /// <summary>
    /// Gets a guild by its ID.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>The guild if found, null otherwise.</returns>
    public async Task<Guild?> GetByIdAsync(Guid guildId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, name, invitation_code, created_at
            FROM guilds
            WHERE id = @GuildId";
        
        return await connection.QuerySingleOrDefaultAsync<Guild>(sql, new { GuildId = guildId });
    }

    /// <summary>
    /// Gets all guilds.
    /// </summary>
    /// <returns>A list of all guilds.</returns>
    public async Task<IEnumerable<Guild>> GetAllAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, name, invitation_code, created_at
            FROM guilds
            ORDER BY name";
        
        return await connection.QueryAsync<Guild>(sql);
    }
}
