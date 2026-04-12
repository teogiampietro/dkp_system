using Microsoft.AspNetCore.Identity;
using DkpSystem.Models;
using DkpSystem.Data.Repositories;

namespace DkpSystem.Data.Identity;

/// <summary>
/// Custom UserStore implementation for ASP.NET Core Identity using Dapper.
/// </summary>
public class DapperUserStore : IUserStore<User>, IUserPasswordStore<User>, IUserRoleStore<User>, IUserEmailStore<User>
{
    private readonly UserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the DapperUserStore class.
    /// </summary>
    /// <param name="userRepository">User repository for data access.</param>
    public DapperUserStore(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Creates a new user in the store.
    /// </summary>
    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.Active = true;
            user.DkpBalance = 0;

            await _userRepository.CreateAsync(user);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing user in the store.
    /// </summary>
    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var success = await _userRepository.UpdateAsync(user);
            return success ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Description = "User update failed" });
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a user from the store (soft delete).
    /// </summary>
    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var success = await _userRepository.DeleteAsync(user.Id);
            return success ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Description = "User deletion failed" });
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    /// <summary>
    /// Finds a user by their unique identifier.
    /// </summary>
    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Guid.TryParse(userId, out var id))
        {
            return null;
        }

        return await _userRepository.FindByIdAsync(id);
    }

    /// <summary>
    /// Finds a user by their username (using email as username).
    /// </summary>
    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userRepository.FindByEmailAsync(normalizedUserName);
    }

    /// <summary>
    /// Gets the user ID as a string.
    /// </summary>
    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.Id.ToString());
    }

    /// <summary>
    /// Gets the username (email) for a user.
    /// </summary>
    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(user.Email);
    }

    /// <summary>
    /// Sets the username (email) for a user.
    /// </summary>
    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.Email = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the normalized username (email) for a user.
    /// </summary>
    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(user.Email.ToUpperInvariant());
    }

    /// <summary>
    /// Sets the normalized username (not stored separately, computed on the fly).
    /// </summary>
    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // We don't store normalized username separately
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the password hash for a user.
    /// </summary>
    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(user.PasswordHash);
    }

    /// <summary>
    /// Sets the password hash for a user.
    /// </summary>
    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.PasswordHash = passwordHash ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a user has a password set.
    /// </summary>
    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    /// <summary>
    /// Adds a user to a role.
    /// </summary>
    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.Role = roleName.ToLower();
        await _userRepository.UpdateRoleAsync(user.Id, user.Role);
    }

    /// <summary>
    /// Removes a user from a role (not applicable in this system).
    /// </summary>
    public Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Not applicable - users have a single role
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the roles for a user.
    /// </summary>
    public Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IList<string> roles = new List<string> { user.Role };
        return Task.FromResult(roles);
    }

    /// <summary>
    /// Checks if a user is in a specific role.
    /// </summary>
    public Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.Role.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets users in a specific role.
    /// </summary>
    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allUsers = await _userRepository.GetAllActiveAsync();
        return allUsers.Where(u => u.Role.Equals(roleName, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Sets the email for a user.
    /// </summary>
    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.Email = email ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the email for a user.
    /// </summary>
    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(user.Email);
    }

    /// <summary>
    /// Gets whether the email is confirmed (always true in this system).
    /// </summary>
    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(true); // Email confirmation not implemented
    }

    /// <summary>
    /// Sets whether the email is confirmed.
    /// </summary>
    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Email confirmation not implemented
        return Task.CompletedTask;
    }

    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userRepository.FindByEmailAsync(normalizedEmail);
    }

    /// <summary>
    /// Gets the normalized email for a user.
    /// </summary>
    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(user.Email.ToUpperInvariant());
    }

    /// <summary>
    /// Sets the normalized email (not stored separately, computed on the fly).
    /// </summary>
    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // We don't store normalized email separately
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the store.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose
    }
}
