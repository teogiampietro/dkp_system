namespace DkpSystem.Models;

/// <summary>
/// Represents a raid event where DKP can be earned.
/// </summary>
public class Event
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the guild identifier this event belongs to.
    /// </summary>
    public Guid GuildId { get; set; }

    /// <summary>
    /// Gets or sets the name of the event.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who created the event.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
