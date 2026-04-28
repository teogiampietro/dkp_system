namespace DkpSystem.Services;

/// <summary>
/// Singleton pub/sub bus for auction state changes. Components subscribe to
/// <see cref="AuctionUpdated"/> and re-render when the relevant auction changes.
/// </summary>
public class AuctionNotificationService
{
    public event Action<Guid>? AuctionUpdated;

    public void NotifyAuctionUpdated(Guid auctionId) =>
        AuctionUpdated?.Invoke(auctionId);
}
