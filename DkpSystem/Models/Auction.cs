namespace DkpSystem.Models;

/// <summary>
/// Represents an auction session for bidding on items with DKP.
/// </summary>
public class Auction
{
    /// <summary>
    /// Gets or sets the unique identifier for the auction.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the guild identifier this auction belongs to.
    /// </summary>
    public Guid GuildId { get; set; }

    /// <summary>
    /// Gets or sets the name of the auction.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description for the auction.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the status of the auction (pending, open, closed, cancelled).
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Gets or sets the scheduled closing time (visual reference only).
    /// </summary>
    public DateTime ClosesAt { get; set; }

    /// <summary>
    /// Gets or sets the actual closing time (set by admin action).
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who created the auction.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the auction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
