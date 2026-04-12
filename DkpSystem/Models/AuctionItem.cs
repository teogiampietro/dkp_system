namespace DkpSystem.Models;

/// <summary>
/// Represents an item within an auction that can be bid on.
/// </summary>
public class AuctionItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the auction item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the auction identifier this item belongs to.
    /// </summary>
    public Guid AuctionId { get; set; }

    /// <summary>
    /// Gets or sets the name of the item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum bid amount for this item.
    /// </summary>
    public int MinimumBid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item has been delivered.
    /// </summary>
    public bool Delivered { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who delivered the item (admin).
    /// </summary>
    public Guid? DeliveredBy { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who won the item.
    /// </summary>
    public Guid? WinnerId { get; set; }

    /// <summary>
    /// Gets or sets the final price paid for the item.
    /// </summary>
    public int? FinalPrice { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
