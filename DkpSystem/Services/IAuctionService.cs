using DkpSystem.Data.Repositories;
using DkpSystem.Models;

namespace DkpSystem.Services;

/// <summary>
/// Service for managing auction business logic including lifecycle, bidding, and delivery.
/// </summary>
public interface IAuctionService
{
    /// <summary>
    /// Creates a new auction with items.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="name">The auction name.</param>
    /// <param name="closesAt">When bidding closes.</param>
    /// <param name="createdBy">The user ID who created the auction.</param>
    /// <param name="items">The list of items with names and minimum bids.</param>
    /// <param name="description">Optional description for the auction.</param>
    /// <returns>The created auction.</returns>
    Task<(bool Success, string ErrorMessage, Auction? Auction)> CreateAuctionAsync(
        Guid guildId,
        string name,
        DateTime closesAt,
        Guid createdBy,
        List<(string Name, int MinimumBid, string? ImageUrl)> items,
        string? description = null);

    /// <summary>
    /// Starts an auction, changing its status to open.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    Task<(bool Success, string ErrorMessage)> StartAuctionAsync(Guid auctionId);

    /// <summary>
    /// Closes an auction, changing its status to closed.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    Task<(bool Success, string ErrorMessage)> CloseAuctionAsync(Guid auctionId);

    /// <summary>
    /// Cancels an auction, discarding all bids.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    Task<(bool Success, string ErrorMessage)> CancelAuctionAsync(Guid auctionId);

    /// <summary>
    /// Places or updates a bid on an auction item.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionItemId">The auction item ID.</param>
    /// <param name="amount">The bid amount.</param>
    /// <param name="bidType">The bid type (main, alt, greed).</param>
    Task<(bool Success, string ErrorMessage)> PlaceOrUpdateBidAsync(
        Guid userId,
        Guid auctionItemId,
        int amount,
        string bidType);

    /// <summary>
    /// Retracts a bid from an auction item.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionItemId">The auction item ID.</param>
    Task<(bool Success, string ErrorMessage)> RetractBidAsync(Guid userId, Guid auctionItemId);

    /// <summary>
    /// Retracts any bid on an open auction item. Admin-only operation.
    /// </summary>
    /// <param name="bidId">The ID of the bid to retract.</param>
    Task<(bool Success, string ErrorMessage)> AdminRetractBidAsync(Guid bidId);

    /// <summary>
    /// Gets sorted bids for an auction item with tiebreaker resolution.
    /// </summary>
    /// <param name="auctionItemId">The auction item ID.</param>
    /// <returns>List of bids with usernames, sorted by priority.</returns>
    Task<List<BidWithUser>> GetSortedBidsForItemAsync(Guid auctionItemId);

    /// <summary>
    /// Delivers an item to the winner, deducting DKP.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="winnerId">The winner's user ID.</param>
    /// <param name="finalPrice">The final price.</param>
    /// <param name="deliveredBy">The admin who delivered the item.</param>
    Task<(bool Success, string ErrorMessage)> DeliverItemAsync(
        Guid itemId,
        Guid winnerId,
        int finalPrice,
        Guid deliveredBy);

    /// <summary>
    /// Gets all auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    Task<IEnumerable<Auction>> GetAuctionsByGuildAsync(Guid guildId);

    /// <summary>
    /// Gets an auction by ID.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    Task<Auction?> GetAuctionByIdAsync(Guid auctionId);

    /// <summary>
    /// Gets all items for an auction.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    Task<IEnumerable<AuctionItem>> GetAuctionItemsAsync(Guid auctionId);

    /// <summary>
    /// Gets a user's bids for an auction.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionId">The auction ID.</param>
    Task<IEnumerable<AuctionBid>> GetUserBidsForAuctionAsync(Guid userId, Guid auctionId);

    /// <summary>
    /// Gets the total active bids for a user in an auction.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionId">The auction ID.</param>
    Task<int> GetTotalActiveBidsAsync(Guid userId, Guid auctionId);

    /// <summary>
    /// Gets closed auctions for a guild (history).
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    Task<IEnumerable<Auction>> GetClosedAuctionsAsync(Guid guildId);

    /// <summary>
    /// Gets all unresolved items for a closed auction — items with no winner (no bids or explicitly skipped).
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    Task<IEnumerable<AuctionItem>> GetUnresolvedItemsAsync(Guid auctionId);

    /// <summary>
    /// Gets the most recently delivered items across all auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="count">Maximum number of items to return.</param>
    Task<IEnumerable<RecentDeliveredItem>> GetRecentDeliveredItemsAsync(Guid guildId, int count = 10);

    /// <summary>
    /// Gets all open auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    Task<IEnumerable<Auction>> GetOpenAuctionsByGuildAsync(Guid guildId);

    /// <summary>
    /// Marks an auction item as skipped (no winner, no DKP deduction).
    /// </summary>
    /// <param name="itemId">The item ID to skip.</param>
    /// <param name="skippedBy">The admin user ID performing the skip.</param>
    Task<(bool Success, string ErrorMessage)> SkipItemAsync(Guid itemId, Guid skippedBy);
}
