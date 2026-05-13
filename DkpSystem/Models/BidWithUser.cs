namespace DkpSystem.Models;

/// <summary>
/// Represents a bid with associated user information.
/// </summary>
public class BidWithUser
{
    /// <summary>
    /// Gets or sets the bid.
    /// </summary>
    public AuctionBid Bid { get; set; } = null!;

    /// <summary>
    /// Gets or sets the username of the bidder.
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
