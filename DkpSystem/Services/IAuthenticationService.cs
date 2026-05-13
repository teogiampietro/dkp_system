using DkpSystem.Models;

namespace DkpSystem.Services;

/// <summary>
/// Service for handling user authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user with email, username, password, and invitation code.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="username">User's display name.</param>
    /// <param name="password">User's password.</param>
    /// <param name="invitationCode">Guild invitation code.</param>
    /// <returns>Result containing success status and any error messages.</returns>
    Task<(bool Success, string[] Errors)> RegisterAsync(string email, string username, string password, string invitationCode);

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's password.</param>
    /// <returns>Result containing success status and any error message.</returns>
    Task<(bool Success, string Error)> LoginAsync(string email, string password);

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Changes the password for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="currentPassword">Current password.</param>
    /// <param name="newPassword">New password.</param>
    /// <returns>Result containing success status and any error messages.</returns>
    Task<(bool Success, string[] Errors)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    /// <summary>
    /// Resets a user's password (admin operation).
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="newPassword">New temporary password.</param>
    /// <returns>Result containing success status and any error messages.</returns>
    Task<(bool Success, string[] Errors)> ResetPasswordAsync(Guid userId, string newPassword);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>User if found, null otherwise.</returns>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Gets a user by their email.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <returns>User if found, null otherwise.</returns>
    Task<User?> GetUserByEmailAsync(string email);
}
