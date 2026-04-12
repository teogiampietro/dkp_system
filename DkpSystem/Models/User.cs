using System.ComponentModel.DataAnnotations.Schema;

namespace DkpSystem.Models;

/// <summary>
/// Represents a user in the DKP system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password of the user.
    /// </summary>
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the user (admin or raider).
    /// </summary>
    public string Role { get; set; } = "raider";

    /// <summary>
    /// Gets or sets the guild identifier the user belongs to.
    /// </summary>
    [Column("guild_id")]
    public Guid? GuildId { get; set; }

    /// <summary>
    /// Gets or sets the current DKP balance of the user.
    /// </summary>
    [Column("dkp_balance")]
    public int DkpBalance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
