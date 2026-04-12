namespace DkpSystem.Models;

/// <summary>
/// Represents DKP earned by a user from an event reward line.
/// </summary>
public class DkpEarning
{
    /// <summary>
    /// Gets or sets the unique identifier for the earning.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who earned the DKP.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the event identifier this earning is associated with.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the reward line identifier this earning is associated with.
    /// </summary>
    public Guid RewardLineId { get; set; }

    /// <summary>
    /// Gets or sets the DKP amount earned.
    /// </summary>
    public int DkpAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the DKP was earned.
    /// </summary>
    public DateTime EarnedAt { get; set; }
}
