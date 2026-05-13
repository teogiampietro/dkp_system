namespace DkpSystem.Services;

/// <summary>
/// Singleton pub/sub bus for auction state changes. Components subscribe to
/// <see cref="AuctionUpdated"/> and re-render when the relevant auction changes.
/// </summary>
public interface IAuctionNotificationService
{
    /// <summary>
    /// Raised when an auction has been updated. The argument is the auction ID.
    /// </summary>
    event Action<Guid>? AuctionUpdated;

    /// <summary>
    /// Notifies subscribers that an auction has been updated.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    void NotifyAuctionUpdated(Guid auctionId);
}
