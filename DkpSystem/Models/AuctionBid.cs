namespace DkpSystem.Models;

/// <summary>
/// Represents a bid placed by a user on an auction item.
/// </summary>
public class AuctionBid
{
    /// <summary>
    /// Gets or sets the unique identifier for the bid.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the auction item identifier this bid is for.
    /// </summary>
    public Guid AuctionItemId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who placed the bid.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the bid amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Gets or sets the bid type (main, alt, greed).
    /// </summary>
    public string BidType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the bid was placed.
    /// </summary>
    public DateTime PlacedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the bid was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
