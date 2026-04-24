using DkpSystem.Data.Repositories;
using DkpSystem.Models;

namespace DkpSystem.Services;

/// <summary>
/// Service for managing auction business logic including lifecycle, bidding, and delivery.
/// </summary>
public class AuctionService
{
    private readonly AuctionRepository _auctionRepository;
    private readonly BidRepository _bidRepository;
    private readonly UserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionService"/> class.
    /// </summary>
    /// <param name="auctionRepository">The auction repository.</param>
    /// <param name="bidRepository">The bid repository.</param>
    /// <param name="userRepository">The user repository.</param>
    public AuctionService(
        AuctionRepository auctionRepository,
        BidRepository bidRepository,
        UserRepository userRepository)
    {
        _auctionRepository = auctionRepository;
        _bidRepository = bidRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Creates a new auction with items.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="name">The auction name.</param>
    /// <param name="durationMinutes">The duration in minutes.</param>
    /// <param name="createdBy">The user ID who created the auction.</param>
    /// <param name="items">The list of items with names and minimum bids.</param>
    /// <returns>The created auction.</returns>
    public async Task<(bool Success, string ErrorMessage, Auction? Auction)> CreateAuctionAsync(
        Guid guildId,
        string name,
        DateTime closesAt,
        Guid createdBy,
        List<(string Name, int MinimumBid, string? ImageUrl)> items)
    {
        // Validate items
        if (items == null || items.Count == 0)
        {
            return (false, "At least one item is required.", null);
        }

        // Check for duplicate item names (case-insensitive)
        var duplicates = items
            .GroupBy(i => i.Name.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            return (false, $"Duplicate item names are not allowed: {string.Join(", ", duplicates)}", null);
        }

        // Validate minimum bids
        if (items.Any(i => i.MinimumBid <= 0))
        {
            return (false, "All minimum bids must be greater than zero.", null);
        }

        // Create the auction
        var auction = new Auction
        {
            GuildId = guildId,
            Name = name,
            Status = "pending",
            ClosesAt = closesAt.ToUniversalTime(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var createdAuction = await _auctionRepository.CreateAuctionAsync(auction);

        // Add items
        foreach (var item in items)
        {
            var auctionItem = new AuctionItem
            {
                AuctionId = createdAuction.Id,
                Name = item.Name,
                MinimumBid = item.MinimumBid,
                ImageUrl = item.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _auctionRepository.AddAuctionItemAsync(auctionItem);
        }

        return (true, string.Empty, createdAuction);
    }

    /// <summary>
    /// Starts an auction, changing its status to open.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <param name="durationMinutes">The duration in minutes.</param>
    public async Task<(bool Success, string ErrorMessage)> StartAuctionAsync(Guid auctionId)
    {
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        if (auction == null)
        {
            return (false, "Auction not found.");
        }

        if (auction.Status != "pending" && auction.Status != "cancelled")
        {
            return (false, "Only pending or cancelled auctions can be started.");
        }

        await _auctionRepository.UpdateAuctionStatusAsync(auctionId, "open", null);

        return (true, string.Empty);
    }

    /// <summary>
    /// Closes an auction, changing its status to closed.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    public async Task<(bool Success, string ErrorMessage)> CloseAuctionAsync(Guid auctionId)
    {
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        if (auction == null)
        {
            return (false, "Auction not found.");
        }

        if (auction.Status != "open")
        {
            return (false, "Only open auctions can be closed.");
        }

        await _auctionRepository.UpdateAuctionStatusAsync(auctionId, "closed", DateTime.UtcNow);

        return (true, string.Empty);
    }

    /// <summary>
    /// Cancels an auction, discarding all bids.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    public async Task<(bool Success, string ErrorMessage)> CancelAuctionAsync(Guid auctionId)
    {
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        if (auction == null)
        {
            return (false, "Auction not found.");
        }

        if (auction.Status == "closed")
        {
            return (false, "Closed auctions cannot be cancelled.");
        }

        // Delete all bids
        await _bidRepository.DeleteBidsByAuctionAsync(auctionId);

        // Update status to cancelled
        await _auctionRepository.UpdateAuctionStatusAsync(auctionId, "cancelled", null);

        return (true, string.Empty);
    }

    /// <summary>
    /// Places or updates a bid on an auction item.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionItemId">The auction item ID.</param>
    /// <param name="amount">The bid amount.</param>
    /// <param name="bidType">The bid type (main, alt, greed).</param>
    public async Task<(bool Success, string ErrorMessage)> PlaceOrUpdateBidAsync(
        Guid userId,
        Guid auctionItemId,
        int amount,
        string bidType)
    {
        // Get the auction item
        var item = await _auctionRepository.GetAuctionItemByIdAsync(auctionItemId);
        if (item == null)
        {
            return (false, "Auction item not found.");
        }

        // Get the auction
        var auction = await _auctionRepository.GetAuctionByIdAsync(item.AuctionId);
        if (auction == null)
        {
            return (false, "Auction not found.");
        }

        // Check auction status
        if (auction.Status != "open")
        {
            return (false, "Bids can only be placed on open auctions.");
        }

        // Validate bid amount
        if (amount < item.MinimumBid)
        {
            return (false, $"Bid amount must be at least {item.MinimumBid} DKP (minimum bid).");
        }

        // Validate bid type
        var validBidTypes = new[] { "main", "collection", "alt", "greed" };
        if (!validBidTypes.Contains(bidType.ToLower()))
        {
            return (false, "Invalid bid type. Must be 'main', 'collection', 'alt', or 'greed'.");
        }

        // Get user's current balance
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        // Check if user already has a bid on this item
        var existingBid = await _bidRepository.GetBidByUserAndItemAsync(userId, auctionItemId);

        // Calculate total active bids excluding this item
        var totalOtherBids = await _bidRepository.GetTotalActiveBidsExcludingItemAsync(userId, auction.Id, auctionItemId);

        // Check if total bids would exceed balance
        if (totalOtherBids + amount > user.DkpBalance)
        {
            return (false, $"Total active bids ({totalOtherBids + amount} DKP) would exceed your balance ({user.DkpBalance} DKP).");
        }

        // Place or update the bid
        if (existingBid == null)
        {
            // Place new bid
            var newBid = new AuctionBid
            {
                AuctionItemId = auctionItemId,
                UserId = userId,
                Amount = amount,
                BidType = bidType.ToLower(),
                PlacedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _bidRepository.PlaceBidAsync(newBid);
        }
        else
        {
            // Update existing bid
            await _bidRepository.UpdateBidAsync(existingBid.Id, amount, bidType.ToLower());
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Retracts a bid from an auction item.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionItemId">The auction item ID.</param>
    public async Task<(bool Success, string ErrorMessage)> RetractBidAsync(Guid userId, Guid auctionItemId)
    {
        // Get the auction item
        var item = await _auctionRepository.GetAuctionItemByIdAsync(auctionItemId);
        if (item == null)
        {
            return (false, "Auction item not found.");
        }

        // Get the auction
        var auction = await _auctionRepository.GetAuctionByIdAsync(item.AuctionId);
        if (auction == null)
        {
            return (false, "Auction not found.");
        }

        // Check auction status
        if (auction.Status != "open")
        {
            return (false, "Bids can only be retracted from open auctions.");
        }

        // Get the bid
        var bid = await _bidRepository.GetBidByUserAndItemAsync(userId, auctionItemId);
        if (bid == null)
        {
            return (false, "No bid found for this item.");
        }

        // Retract the bid
        await _bidRepository.RetractBidAsync(bid.Id);

        return (true, string.Empty);
    }

    /// <summary>
    /// Gets sorted bids for an auction item with tiebreaker resolution.
    /// </summary>
    /// <param name="auctionItemId">The auction item ID.</param>
    /// <returns>List of bids with usernames, sorted by priority.</returns>
    public async Task<List<BidWithUser>> GetSortedBidsForItemAsync(Guid auctionItemId)
    {
        var bids = await _bidRepository.GetBidsByItemAsync(auctionItemId);
        var bidList = new List<BidWithUser>();

        foreach (var bid in bids)
        {
            var user = await _userRepository.FindByIdAsync(bid.UserId);
            if (user != null)
            {
                bidList.Add(new BidWithUser
                {
                    Bid = bid,
                    Username = user.Username
                });
            }
        }

        // Sort by bid type priority first (Main > Alt > Greed), then by amount descending, then by placed_at
        var sorted = bidList
            .OrderBy(b => GetBidTypePriority(b.Bid.BidType))
            .ThenByDescending(b => b.Bid.Amount)
            .ThenBy(b => b.Bid.PlacedAt)
            .ToList();

        return sorted;
    }

    /// <summary>
    /// Delivers an item to the winner, deducting DKP.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="winnerId">The winner's user ID.</param>
    /// <param name="finalPrice">The final price.</param>
    /// <param name="deliveredBy">The admin who delivered the item.</param>
    public async Task<(bool Success, string ErrorMessage)> DeliverItemAsync(
        Guid itemId,
        Guid winnerId,
        int finalPrice,
        Guid deliveredBy)
    {
        // Get the item
        var item = await _auctionRepository.GetAuctionItemByIdAsync(itemId);
        if (item == null)
        {
            return (false, "Item not found.");
        }

        // Get the auction
        var auction = await _auctionRepository.GetAuctionByIdAsync(item.AuctionId);
        if (auction == null)
        {
            return (false, "Auction not found.");
        }

        // Check auction status
        if (auction.Status != "closed")
        {
            return (false, "Items can only be delivered from closed auctions.");
        }

        // Check if already delivered
        if (item.Delivered)
        {
            return (false, "This item has already been delivered.");
        }

        // Get winner's balance
        var winner = await _userRepository.FindByIdAsync(winnerId);
        if (winner == null)
        {
            return (false, "Winner not found.");
        }

        // Check if winner has enough balance
        if (winner.DkpBalance < finalPrice)
        {
            return (false, $"Winner's balance ({winner.DkpBalance} DKP) is insufficient for this bid ({finalPrice} DKP).");
        }

        // Deliver the item (this updates the item and deducts DKP in a transaction)
        await _auctionRepository.DeliverItemAsync(itemId, winnerId, finalPrice, deliveredBy);

        return (true, string.Empty);
    }

    /// <summary>
    /// Gets the bid type priority for sorting (lower is higher priority).
    /// </summary>
    private int GetBidTypePriority(string bidType)
    {
        return bidType.ToLower() switch
        {
            "main" => 1,
            "collection" => 2,
            "alt" => 3,
            "greed" => 4,
            _ => 5
        };
    }

    /// <summary>
    /// Gets all auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    public async Task<IEnumerable<Auction>> GetAuctionsByGuildAsync(Guid guildId)
    {
        return await _auctionRepository.GetAuctionsByGuildAsync(guildId);
    }

    /// <summary>
    /// Gets an auction by ID.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    public async Task<Auction?> GetAuctionByIdAsync(Guid auctionId)
    {
        return await _auctionRepository.GetAuctionByIdAsync(auctionId);
    }

    /// <summary>
    /// Gets all items for an auction.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    public async Task<IEnumerable<AuctionItem>> GetAuctionItemsAsync(Guid auctionId)
    {
        return await _auctionRepository.GetAuctionItemsAsync(auctionId);
    }

    /// <summary>
    /// Gets a user's bids for an auction.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionId">The auction ID.</param>
    public async Task<IEnumerable<AuctionBid>> GetUserBidsForAuctionAsync(Guid userId, Guid auctionId)
    {
        return await _bidRepository.GetBidsByUserAndAuctionAsync(userId, auctionId);
    }

    /// <summary>
    /// Gets the total active bids for a user in an auction.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionId">The auction ID.</param>
    public async Task<int> GetTotalActiveBidsAsync(Guid userId, Guid auctionId)
    {
        return await _bidRepository.GetTotalActiveBidsAsync(userId, auctionId);
    }

    /// <summary>
    /// Gets closed auctions for a guild (history).
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    public async Task<IEnumerable<Auction>> GetClosedAuctionsAsync(Guid guildId)
    {
        return await _auctionRepository.GetClosedAuctionsByGuildAsync(guildId);
    }

    /// <summary>
    /// Gets all unresolved items for a closed auction — items with no winner (no bids or explicitly skipped).
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    public async Task<IEnumerable<AuctionItem>> GetUnresolvedItemsAsync(Guid auctionId)
    {
        return await _auctionRepository.GetUnresolvedItemsAsync(auctionId);
    }

    /// <summary>
    /// Gets the most recently delivered items across all auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="count">Maximum number of items to return.</param>
    public async Task<IEnumerable<RecentDeliveredItem>> GetRecentDeliveredItemsAsync(Guid guildId, int count = 10)
    {
        return await _auctionRepository.GetRecentDeliveredItemsAsync(guildId, count);
    }

    /// <summary>
    /// Gets all open auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    public async Task<IEnumerable<Auction>> GetOpenAuctionsByGuildAsync(Guid guildId)
    {
        return await _auctionRepository.GetOpenAuctionsByGuildAsync(guildId);
    }

    /// <summary>
    /// Marks an auction item as skipped (no winner, no DKP deduction).
    /// </summary>
    /// <param name="itemId">The item ID to skip.</param>
    /// <param name="skippedBy">The admin user ID performing the skip.</param>
    public async Task<(bool Success, string ErrorMessage)> SkipItemAsync(Guid itemId, Guid skippedBy)
    {
        var item = await _auctionRepository.GetAuctionItemByIdAsync(itemId);
        if (item == null)
        {
            return (false, "Item not found.");
        }

        await _auctionRepository.SkipItemAsync(itemId, skippedBy);
        return (true, string.Empty);
    }
}

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
