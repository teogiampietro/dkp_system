using Dapper;
using DkpSystem.Models;

namespace DkpSystem.Data.Repositories;

/// <summary>
/// Repository for user data access operations using Dapper.
/// </summary>
public class UserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the UserRepository class.
    /// </summary>
    /// <param name="connectionFactory">Database connection factory.</param>
    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    /// <param name="user">User to create.</param>
    /// <returns>The created user with generated ID.</returns>
    public async Task<User> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at)
            VALUES (@Id, @Email, @Username, @PasswordHash, @Role, @GuildId, @DkpBalance, @Active, @CreatedAt)
            RETURNING *";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var createdUser = await connection.QuerySingleAsync<User>(sql, user);
        return createdUser;
    }

    /// <summary>
    /// Finds a user by their unique identifier.
    /// </summary>
    /// <param name="userId">User ID to search for.</param>
    /// <returns>User if found, null otherwise.</returns>
    public async Task<User?> FindByIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM users WHERE id = @UserId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    /// <param name="email">Email address to search for.</param>
    /// <returns>User if found, null otherwise.</returns>
    public async Task<User?> FindByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM users WHERE LOWER(email) = LOWER(@Email)";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }

    /// <summary>
    /// Updates an existing user in the database.
    /// </summary>
    /// <param name="user">User with updated information.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE users 
            SET email = @Email,
                username = @Username,
                password_hash = @PasswordHash,
                role = @Role,
                guild_id = @GuildId,
                dkp_balance = @DkpBalance,
                active = @Active
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, user);
        return rowsAffected > 0;
    }

    /// <summary>
    /// Deletes a user from the database (soft delete by setting active = false).
    /// </summary>
    /// <param name="userId">ID of the user to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    public async Task<bool> DeleteAsync(Guid userId)
    {
        const string sql = "UPDATE users SET active = false WHERE id = @UserId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Gets all active users in the system.
    /// </summary>
    /// <returns>List of all active users.</returns>
    public async Task<IEnumerable<User>> GetAllActiveAsync()
    {
        const string sql = "SELECT * FROM users WHERE active = true ORDER BY username";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<User>(sql);
    }

    /// <summary>
    /// Gets all users in the system, including inactive ones.
    /// </summary>
    /// <returns>List of all users.</returns>
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = "SELECT * FROM users ORDER BY username";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<User>(sql);
    }

    /// <summary>
    /// Updates the password hash for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="passwordHash">New password hash.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    public async Task<bool> UpdatePasswordHashAsync(Guid userId, string passwordHash)
    {
        const string sql = "UPDATE users SET password_hash = @PasswordHash WHERE id = @UserId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId, PasswordHash = passwordHash });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Updates the role for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="role">New role.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    public async Task<bool> UpdateRoleAsync(Guid userId, string role)
    {
        const string sql = "UPDATE users SET role = @Role WHERE id = @UserId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId, Role = role });
        return rowsAffected > 0;
    }

    /// <summary>
    /// Checks if an email is already registered.
    /// </summary>
    /// <param name="email">Email to check.</param>
    /// <returns>True if email exists, false otherwise.</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE LOWER(email) = LOWER(@Email)";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }
}
