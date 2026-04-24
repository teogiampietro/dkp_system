using Dapper;
using DkpSystem.Models;

namespace DkpSystem.Data.Repositories;

/// <summary>
/// Represents a recently delivered auction item with winner and auction info, used for dashboard display.
/// </summary>
public class RecentDeliveredItem
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid AuctionId { get; set; }
    public string AuctionName { get; set; } = string.Empty;
    public string WinnerName { get; set; } = string.Empty;
    public int DkpPaid { get; set; }
    public DateTime DeliveredAt { get; set; }
}

/// <summary>
/// Repository for managing auction and auction item data access.
/// </summary>
public class AuctionRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public AuctionRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Creates a new auction.
    /// </summary>
    /// <param name="auction">The auction to create.</param>
    /// <returns>The created auction with generated ID.</returns>
    public async Task<Auction> CreateAuctionAsync(Auction auction)
    {
        const string sql = @"
            INSERT INTO auctions (guild_id, name, status, closes_at, created_by, created_at)
            VALUES (@GuildId, @Name, @Status, @ClosesAt, @CreatedBy, @CreatedAt)
            RETURNING id, guild_id, name, status, closes_at, closed_at, created_by, created_at";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QuerySingleAsync<Auction>(sql, auction);
        return result;
    }

    /// <summary>
    /// Gets an auction by ID.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <returns>The auction if found, null otherwise.</returns>
    public async Task<Auction?> GetAuctionByIdAsync(Guid auctionId)
    {
        const string sql = @"
            SELECT id, guild_id, name, status, closes_at, closed_at, created_by, created_at
            FROM auctions
            WHERE id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Auction>(sql, new { AuctionId = auctionId });
    }

    /// <summary>
    /// Gets all auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>List of auctions.</returns>
    public async Task<IEnumerable<Auction>> GetAuctionsByGuildAsync(Guid guildId)
    {
        const string sql = @"
            SELECT id, guild_id, name, status, closes_at, closed_at, created_by, created_at
            FROM auctions
            WHERE guild_id = @GuildId
            ORDER BY created_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<Auction>(sql, new { GuildId = guildId });
    }

    /// <summary>
    /// Gets all open auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>List of open auctions.</returns>
    public async Task<IEnumerable<Auction>> GetOpenAuctionsByGuildAsync(Guid guildId)
    {
        const string sql = @"
            SELECT id, guild_id, name, status, closes_at, closed_at, created_by, created_at
            FROM auctions
            WHERE guild_id = @GuildId AND status = 'open'
            ORDER BY created_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<Auction>(sql, new { GuildId = guildId });
    }

    /// <summary>
    /// Gets all closed auctions for a guild (history).
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>List of closed auctions.</returns>
    public async Task<IEnumerable<Auction>> GetClosedAuctionsByGuildAsync(Guid guildId)
    {
        const string sql = @"
            SELECT id, guild_id, name, status, closes_at, closed_at, created_by, created_at
            FROM auctions
            WHERE guild_id = @GuildId AND status = 'closed'
            ORDER BY closed_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<Auction>(sql, new { GuildId = guildId });
    }

    /// <summary>
    /// Updates an auction's status.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="closedAt">The closing timestamp (optional).</param>
    public async Task UpdateAuctionStatusAsync(Guid auctionId, string status, DateTime? closedAt = null)
    {
        const string sql = @"
            UPDATE auctions
            SET status = @Status, closed_at = @ClosedAt
            WHERE id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { AuctionId = auctionId, Status = status, ClosedAt = closedAt });
    }

    /// <summary>
    /// Updates an auction's closes_at timestamp.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <param name="closesAt">The new scheduled closing time.</param>
    public async Task UpdateAuctionClosesAtAsync(Guid auctionId, DateTime closesAt)
    {
        const string sql = @"
            UPDATE auctions
            SET closes_at = @ClosesAt
            WHERE id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { AuctionId = auctionId, ClosesAt = closesAt });
    }

    /// <summary>
    /// Adds an item to an auction.
    /// </summary>
    /// <param name="item">The auction item to add.</param>
    /// <returns>The created item with generated ID.</returns>
    public async Task<AuctionItem> AddAuctionItemAsync(AuctionItem item)
    {
        const string sql = @"
            INSERT INTO auction_items (auction_id, name, minimum_bid, image_url, created_at)
            VALUES (@AuctionId, @Name, @MinimumBid, @ImageUrl, @CreatedAt)
            RETURNING id, auction_id, name, minimum_bid, image_url, delivered, delivered_at, delivered_by, winner_id, final_price, created_at";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QuerySingleAsync<AuctionItem>(sql, item);
        return result;
    }

    /// <summary>
    /// Gets all items for an auction.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <returns>List of auction items.</returns>
    public async Task<IEnumerable<AuctionItem>> GetAuctionItemsAsync(Guid auctionId)
    {
        const string sql = @"
            SELECT id, auction_id, name, minimum_bid, image_url, delivered, delivered_at, delivered_by, winner_id, final_price, created_at
            FROM auction_items
            WHERE auction_id = @AuctionId
            ORDER BY created_at";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<AuctionItem>(sql, new { AuctionId = auctionId });
    }

    /// <summary>
    /// Gets all unresolved items for an auction — items with no winner (no bids or explicitly skipped).
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <returns>List of items without a winner.</returns>
    public async Task<IEnumerable<AuctionItem>> GetUnresolvedItemsAsync(Guid auctionId)
    {
        const string sql = @"
            SELECT id, auction_id, name, minimum_bid, image_url, delivered, delivered_at, delivered_by, winner_id, final_price, created_at
            FROM auction_items
            WHERE auction_id = @AuctionId AND winner_id IS NULL
            ORDER BY created_at";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<AuctionItem>(sql, new { AuctionId = auctionId });
    }

    /// <summary>
    /// Gets an auction item by ID.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>The auction item if found, null otherwise.</returns>
    public async Task<AuctionItem?> GetAuctionItemByIdAsync(Guid itemId)
    {
        const string sql = @"
            SELECT id, auction_id, name, minimum_bid, image_url, delivered, delivered_at, delivered_by, winner_id, final_price, created_at
            FROM auction_items
            WHERE id = @ItemId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<AuctionItem>(sql, new { ItemId = itemId });
    }

    /// <summary>
    /// Checks if an auction has duplicate item names.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <param name="itemName">The item name to check.</param>
    /// <returns>True if duplicate exists, false otherwise.</returns>
    public async Task<bool> HasDuplicateItemNameAsync(Guid auctionId, string itemName)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM auction_items
            WHERE auction_id = @AuctionId AND LOWER(name) = LOWER(@ItemName)";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { AuctionId = auctionId, ItemName = itemName });
        return count > 0;
    }

    /// <summary>
    /// Delivers an item to a winner within a transaction.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="winnerId">The winner's user ID.</param>
    /// <param name="finalPrice">The final price paid.</param>
    /// <param name="deliveredBy">The admin who delivered the item.</param>
    public async Task DeliverItemAsync(Guid itemId, Guid winnerId, int finalPrice, Guid deliveredBy)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Update the auction item
            const string updateItemSql = @"
                UPDATE auction_items
                SET delivered = true, delivered_at = @DeliveredAt, delivered_by = @DeliveredBy, 
                    winner_id = @WinnerId, final_price = @FinalPrice
                WHERE id = @ItemId";

            await connection.ExecuteAsync(updateItemSql, new
            {
                ItemId = itemId,
                WinnerId = winnerId,
                FinalPrice = finalPrice,
                DeliveredBy = deliveredBy,
                DeliveredAt = DateTime.UtcNow
            }, transaction);

            // Deduct DKP from winner's balance
            const string updateBalanceSql = @"
                UPDATE users
                SET dkp_balance = dkp_balance - @FinalPrice
                WHERE id = @WinnerId";

            await connection.ExecuteAsync(updateBalanceSql, new { WinnerId = winnerId, FinalPrice = finalPrice }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Marks an item as skipped — delivered with no winner and no DKP deduction.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="skippedBy">The admin user ID performing the skip.</param>
    public async Task SkipItemAsync(Guid itemId, Guid skippedBy)
    {
        const string sql = @"
            UPDATE auction_items
            SET delivered = true, delivered_at = @DeliveredAt, delivered_by = @DeliveredBy
            WHERE id = @ItemId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { ItemId = itemId, DeliveredAt = DateTime.UtcNow, DeliveredBy = skippedBy });
    }

    /// <summary>
    /// Deletes all items for an auction (used when cancelling).
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    public async Task DeleteAuctionItemsAsync(Guid auctionId)
    {
        const string sql = "DELETE FROM auction_items WHERE auction_id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { AuctionId = auctionId });
    }

    /// <summary>
    /// Gets the count of items in an auction.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <returns>The number of items.</returns>
    public async Task<int> GetAuctionItemCountAsync(Guid auctionId)
    {
        const string sql = "SELECT COUNT(*) FROM auction_items WHERE auction_id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(sql, new { AuctionId = auctionId });
    }

    /// <summary>
    /// Gets the most recently delivered items across all auctions for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="count">Maximum number of items to return.</param>
    /// <returns>List of recently delivered items with winner and auction details.</returns>
    public async Task<IEnumerable<RecentDeliveredItem>> GetRecentDeliveredItemsAsync(Guid guildId, int count = 10)
    {
        const string sql = @"
            SELECT
                ai.id AS ItemId,
                ai.name AS ItemName,
                a.id AS AuctionId,
                a.name AS AuctionName,
                u.username AS WinnerName,
                ai.final_price AS DkpPaid,
                ai.delivered_at AS DeliveredAt
            FROM auction_items ai
            INNER JOIN auctions a ON ai.auction_id = a.id
            INNER JOIN users u ON ai.winner_id = u.id
            WHERE ai.delivered = true AND ai.winner_id IS NOT NULL AND a.guild_id = @GuildId
            ORDER BY ai.delivered_at DESC
            LIMIT @Count";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<RecentDeliveredItem>(sql, new { GuildId = guildId, Count = count });
    }

    /// <summary>
    /// Gets the count of bids in an auction.
    /// </summary>
    /// <param name="auctionId">The auction ID.</param>
    /// <returns>The number of bids.</returns>
    public async Task<int> GetAuctionBidCountAsync(Guid auctionId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM auction_bids ab
            INNER JOIN auction_items ai ON ab.auction_item_id = ai.id
            WHERE ai.auction_id = @AuctionId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(sql, new { AuctionId = auctionId });
    }
}
