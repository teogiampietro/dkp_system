namespace DkpSystem.Models;

/// <summary>
/// Represents a member's won item history entry.
/// </summary>
public class MemberWonItemHistory
{
    /// <summary>
    /// Gets or sets the auction name.
    /// </summary>
    public string AuctionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount paid for the item.
    /// </summary>
    public int DkpPaid { get; set; }

    /// <summary>
    /// Gets or sets the date when the item was won.
    /// </summary>
    public DateTime WonAt { get; set; }
}
