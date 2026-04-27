using DkpSystem.Data.Repositories;

namespace DkpSystem.Services;

/// <summary>
/// Background service that automatically closes open auctions once their closes_at time has passed.
/// Runs every 60 seconds.
/// </summary>
public class AuctionAutoCloseService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionAutoCloseService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionAutoCloseService"/> class.
    /// </summary>
    public AuctionAutoCloseService(IServiceScopeFactory scopeFactory, ILogger<AuctionAutoCloseService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<AuctionRepository>();
                var closedIds = (await repository.CloseExpiredAuctionsAsync()).ToList();

                if (closedIds.Count > 0)
                {
                    _logger.LogInformation("Auto-closed {Count} auction(s): {Ids}",
                        closedIds.Count, string.Join(", ", closedIds));
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error while auto-closing expired auctions");
            }
        }
    }
}
