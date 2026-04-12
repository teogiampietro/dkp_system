using Dapper;
using DkpSystem.Models;

namespace DkpSystem.Data.Repositories;

/// <summary>
/// Repository for managing auction bid data access.
/// </summary>
public class BidRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BidRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public BidRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Places a new bid on an auction item.
    /// </summary>
    /// <param name="bid">The bid to place.</param>
    /// <returns>The created bid with generated ID.</returns>
    public async Task<AuctionBid> PlaceBidAsync(AuctionBid bid)
    {
        const string sql = @"
            INSERT INTO auction_bids (auction_item_id, user_id, amount, bid_type, placed_at, updated_at)
            VALUES (@AuctionItemId, @UserId, @Amount, @BidType, @PlacedAt, @UpdatedAt)
            RETURNING id, auction_item_id, user_id, amount, bid_type, placed_at, updated_at";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QuerySingleAsync<AuctionBid>(sql, bid);
        return result;
    }

    /// <summary>
    /// Updates an existing bid.
    /// </summary>
    /// <param name="bidId">The bid ID.</param>
    /// <param name="amount">The new bid amount.</param>
    /// <param name="bidType">The new bid type.</param>
    public async Task UpdateBidAsync(Guid bidId, int amount, string bidType)
    {
        const string sql = @"
            UPDATE auction_bids
            SET amount = @Amount, bid_type = @BidType, updated_at = @UpdatedAt
            WHERE id = @BidId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { BidId = bidId, Amount = amount, BidType = bidType, UpdatedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Retracts (deletes) a bid.
    /// </summary>
    /// <param name="bidId">The bid ID to retract.</param>
    public async Task RetractBidAsync(Guid bidId)
    {
        const string sql = "DELETE FROM auction_bids WHERE id = @BidId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { BidId = bidId });
    }

    /// <summary>
    /// Gets a bid by user and auction item.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionItemId">The auction item ID.</param>
    /// <returns>The bid if found, null otherwise.</returns>
    public async Task<AuctionBid?> GetBidByUserAndItemAsync(Guid userId, Guid auctionItemId)
    {
        const string sql = @"
            SELECT id, auction_item_id, user_id, amount, bid_type, placed_at, updated_at
            FROM auction_bids
            WHERE user_id = @UserId AND auction_item_id = @AuctionItemId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<AuctionBid>(sql, new { UserId = userId, AuctionItemId = auctionItemId });
    }

    /// <summary>
    /// Gets all bids for an auction item.
    /// </summary>
    /// <param name="auctionItemId">The auction item ID.</param>
    /// <returns>List of bids.</returns>
    public async Task<IEnumerable<AuctionBid>> GetBidsByItemAsync(Guid auctionItemId)
    {
        const string sql = @"
            SELECT id, auction_item_id, user_id, amount, bid_type, placed_at, updated_at
            FROM auction_bids
            WHERE auction_item_id = @AuctionItemId
            ORDER BY amount DESC, placed_at ASC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<AuctionBid>(sql, new { AuctionItemId = auctionItemId });
    }

    /// <summary>
    /// Gets all bids by a user for a specific auction.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionId">The auction ID.</param>
    /// <returns>List of bids.</returns>
    public async Task<IEnumerable<AuctionBid>> GetBidsByUserAndAuctionAsync(Guid userId, Guid auctionId)
    {
        const string sql = @"
            SELECT ab.id, ab.auction_item_id, ab.user_id, ab.amount, ab.bid_type, ab.placed_at, ab.updated_at
            FROM auction_bids ab
            INNER JOIN auction_items ai ON ab.auction_item_id = ai.id
            WHERE ab.user_id = @UserId AND ai.auction_id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<AuctionBid>(sql, new { UserId = userId, AuctionId = auctionId });
    }

    /// <summary>
    /// Gets the total active bid amount for a user in an auction.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionId">The auction ID.</param>
    /// <returns>The total bid amount.</returns>
    public async Task<int> GetTotalActiveBidsAsync(Guid userId, Guid auctionId)
    {
        const string sql = @"
            SELECT COALESCE(SUM(ab.amount), 0)
            FROM auction_bids ab
            INNER JOIN auction_items ai ON ab.auction_item_id = ai.id
            WHERE ab.user_id = @UserId AND ai.auction_id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, AuctionId = auctionId });
    }

    /// <summary>
    /// Gets the total active bid amount for a user in an auction, excluding a specific item.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="auctionId">The auction ID.</param>
    /// <param name="excludeItemId">The item ID to exclude from the calculation.</param>
    /// <returns>The total bid amount excluding the specified item.</returns>
    public async Task<int> GetTotalActiveBidsExcludingItemAsync(Guid userId, Guid auctionId, Guid excludeItemId)
    {
        const string sql = @"
            SELECT COALESCE(SUM(ab.amount), 0)
            FROM auction_bids ab
            INNER JOIN auction_items ai ON ab.auction_item_id = ai.id
            WHERE ab.user_id = @UserId AND ai.auction_id = @AuctionId AND ai.id != @ExcludeItemId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, AuctionId = auctionId, ExcludeItemId = excludeItemId });
    }

    /// <summary>
    /// Deletes all bids for an auction (used when cancelling).
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    public async Task DeleteBidsByAuctionAsync(Guid auctionId)
    {
        const string sql = @"
            DELETE FROM auction_bids
            WHERE auction_item_id IN (
                SELECT id FROM auction_items WHERE auction_id = @AuctionId
            )";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { AuctionId = auctionId });
    }

    /// <summary>
    /// Gets a bid by ID.
    /// </summary>
    /// <param name="bidId">The bid ID.</param>
    /// <returns>The bid if found, null otherwise.</returns>
    public async Task<AuctionBid?> GetBidByIdAsync(Guid bidId)
    {
        const string sql = @"
            SELECT id, auction_item_id, user_id, amount, bid_type, placed_at, updated_at
            FROM auction_bids
            WHERE id = @BidId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<AuctionBid>(sql, new { BidId = bidId });
    }

    /// <summary>
    /// Gets the highest bid for an auction item.
    /// </summary>
    /// <param name="auctionItemId">The auction item ID.</param>
    /// <returns>The highest bid if any exist, null otherwise.</returns>
    public async Task<AuctionBid?> GetHighestBidForItemAsync(Guid auctionItemId)
    {
        const string sql = @"
            SELECT id, auction_item_id, user_id, amount, bid_type, placed_at, updated_at
            FROM auction_bids
            WHERE auction_item_id = @AuctionItemId
            ORDER BY amount DESC, placed_at ASC
            LIMIT 1";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<AuctionBid>(sql, new { AuctionItemId = auctionItemId });
    }
}
