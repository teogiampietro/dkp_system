namespace DkpSystem.Models;

/// <summary>
/// Award history entry for admin view.
/// </summary>
public class AwardHistoryEntry
{
    /// <summary>
    /// Gets or sets the reward line ID.
    /// </summary>
    public Guid RewardLineId { get; set; }

    /// <summary>
    /// Gets or sets the reason for the award.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount.
    /// </summary>
    public int DkpAmount { get; set; }

    /// <summary>
    /// Gets or sets whether this is a group award.
    /// </summary>
    public bool IsGroupAward { get; set; }

    /// <summary>
    /// Gets or sets the number of recipients.
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
