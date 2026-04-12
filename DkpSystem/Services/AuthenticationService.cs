using Microsoft.AspNetCore.Identity;
using DkpSystem.Models;
using DkpSystem.Data.Repositories;

namespace DkpSystem.Services;

/// <summary>
/// Service for handling user authentication operations.
/// </summary>
public class AuthenticationService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly UserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the AuthenticationService class.
    /// </summary>
    /// <param name="userManager">User manager for Identity operations.</param>
    /// <param name="signInManager">Sign-in manager for authentication.</param>
    /// <param name="userRepository">User repository for data access.</param>
    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        UserRepository userRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Registers a new user with email, username, and password.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="username">User's display name.</param>
    /// <param name="password">User's password.</param>
    /// <returns>Result containing success status and any error messages.</returns>
    public async Task<(bool Success, string[] Errors)> RegisterAsync(string email, string username, string password)
    {
        // Check if email already exists
        var existingUser = await _userRepository.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return (false, new[] { "Email is already registered." });
        }

        // Create new user
        var user = new User
        {
            Email = email,
            Username = username,
            Role = "raider", // Default role
            GuildId = null,
            DkpBalance = 0,
            Active = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        // Automatically sign in the user after registration
        await _signInManager.SignInAsync(user, isPersistent: false);

        return (true, Array.Empty<string>());
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's password.</param>
    /// <returns>Result containing success status and any error message.</returns>
    public async Task<(bool Success, string Error)> LoginAsync(string email, string password)
    {
        try
        {
            // Find user by email
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null)
            {
                Console.WriteLine($"[LOGIN] User not found for email: {email}");
                return (false, "Invalid credentials.");
            }

            Console.WriteLine($"[LOGIN] User found: {user.Email}, Active: {user.Active}, HasPasswordHash: {!string.IsNullOrEmpty(user.PasswordHash)}");

            // Check if user is active
            if (!user.Active)
            {
                Console.WriteLine($"[LOGIN] User is not active");
                return (false, "This account has been deactivated.");
            }

            // Verify password using UserManager
            Console.WriteLine($"[LOGIN] Checking password...");
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            Console.WriteLine($"[LOGIN] Password valid: {passwordValid}");
            
            if (!passwordValid)
            {
                return (false, "Invalid credentials.");
            }

            // Sign in the user
            Console.WriteLine($"[LOGIN] Signing in user...");
            await _signInManager.SignInAsync(user, isPersistent: false);
            Console.WriteLine($"[LOGIN] Sign in successful!");

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOGIN ERROR] {ex.Message}");
            Console.WriteLine($"[LOGIN ERROR] Stack: {ex.StackTrace}");
            return (false, $"Login error: {ex.Message}");
        }
    }

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    /// <summary>
    /// Changes the password for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="currentPassword">Current password.</param>
    /// <param name="newPassword">New password.</param>
    /// <returns>Result containing success status and any error messages.</returns>
    public async Task<(bool Success, string[] Errors)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, new[] { "User not found." });
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        return (true, Array.Empty<string>());
    }

    /// <summary>
    /// Resets a user's password (admin operation).
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="newPassword">New temporary password.</param>
    /// <returns>Result containing success status and any error messages.</returns>
    public async Task<(bool Success, string[] Errors)> ResetPasswordAsync(Guid userId, string newPassword)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, new[] { "User not found." });
        }

        // Remove existing password
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return (false, removeResult.Errors.Select(e => e.Description).ToArray());
        }

        // Add new password
        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
        {
            return (false, addResult.Errors.Select(e => e.Description).ToArray());
        }

        return (true, Array.Empty<string>());
    }

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>User if found, null otherwise.</returns>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _userRepository.FindByIdAsync(userId);
    }

    /// <summary>
    /// Gets a user by their email.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <returns>User if found, null otherwise.</returns>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.FindByEmailAsync(email);
    }
}
