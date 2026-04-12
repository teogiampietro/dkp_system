namespace DkpSystem.Models;

/// <summary>
/// Represents a reward line within an event (e.g., "Kill dragon +15").
/// </summary>
public class EventRewardLine
{
    /// <summary>
    /// Gets or sets the unique identifier for the reward line.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event identifier this reward line belongs to.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the reason for the reward.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount for this reward.
    /// </summary>
    public int DkpAmount { get; set; }
}
