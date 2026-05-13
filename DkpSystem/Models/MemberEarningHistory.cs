namespace DkpSystem.Models;

/// <summary>
/// Represents a member's DKP earning history entry.
/// </summary>
public class MemberEarningHistory
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the DKP award.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount earned.
    /// </summary>
    public int DkpAmount { get; set; }

    /// <summary>
    /// Gets or sets the date when the DKP was earned.
    /// </summary>
    public DateTime EarnedAt { get; set; }
}
