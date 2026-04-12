namespace DkpSystem.Models;

/// <summary>
/// Represents a guild in the DKP system.
/// </summary>
public class Guild
{
    /// <summary>
    /// Gets or sets the unique identifier for the guild.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the guild.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the guild was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
