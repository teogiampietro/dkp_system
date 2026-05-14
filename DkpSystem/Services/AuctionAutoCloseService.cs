using DkpSystem.Data.Repositories;

namespace DkpSystem.Services;

/// <summary>
/// Background service that automatically closes open auctions once their closes_at time has passed.
/// Polls adaptively: 1s when the soonest close is within 30s, 5s within 5min, 60s otherwise.
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
            TimeSpan delay;
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

                delay = await ComputeNextDelayAsync(repository);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error while auto-closing expired auctions");
                delay = TimeSpan.FromSeconds(60);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task<TimeSpan> ComputeNextDelayAsync(AuctionRepository repository)
    {
        var nextClose = await repository.GetNextOpenCloseTimeAsync();
        if (nextClose == null) return TimeSpan.FromSeconds(60);

        var secondsLeft = (nextClose.Value - DateTime.UtcNow).TotalSeconds;
        if (secondsLeft <= 0) return TimeSpan.FromSeconds(1);
        if (secondsLeft < 30) return TimeSpan.FromSeconds(1);
        if (secondsLeft < 300) return TimeSpan.FromSeconds(5);
        return TimeSpan.FromSeconds(60);
    }
}
