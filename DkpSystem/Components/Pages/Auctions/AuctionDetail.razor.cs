using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using DkpSystem.Models;
using DkpSystem.Services;
using DkpSystem.Data.Repositories;

namespace DkpSystem.Components.Pages.Auctions;

public partial class AuctionDetail : ComponentBase, IAsyncDisposable
{
    [Inject] private IAuctionService AuctionService { get; set; } = default!;
    [Inject] private IAuctionNotificationService NotificationService { get; set; } = default!;
    [Inject] private UserRepository UserRepository { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [Parameter]
    public Guid AuctionId { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private Auction? _auction;
    private List<AuctionItem> _items = new();
    private Dictionary<Guid, List<BidWithUser>> _itemBids = new();
    private Dictionary<Guid, AuctionBid> _userBids = new();
    private Dictionary<Guid, User> _users = new();
    private Dictionary<Guid, int> _bidAmounts = new();
    private Dictionary<Guid, string> _bidTypes = new();
    private User? _currentUser;
    private int _totalActiveBids = 0;
    private bool _isAdmin = false;
    private bool _isOfficer = false;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private string _errorMessage = string.Empty;
    private bool _showSummaryModal = false;
    private bool _copiedToClipboard = false;
    private int _browserUtcOffsetMinutes = 0;
    private bool _suppressUpdateToast = false;
    private System.Threading.Timer? _countdownTimer;

    protected override async Task OnInitializedAsync()
    {
        NotificationService.AuctionUpdated += OnAuctionUpdated;
        await LoadData();
        StartCountdownTimer();
    }

    private void StartCountdownTimer()
    {
        _countdownTimer?.Dispose();
        _countdownTimer = new System.Threading.Timer(_ =>
        {
            if (_auction?.Status != "open") return;
            InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void OnAuctionUpdated(Guid auctionId)
    {
        if (auctionId != AuctionId) return;
        InvokeAsync(async () =>
        {
            var previousClosesAt = _auction?.ClosesAt;
            await LoadData(silent: true);
            // Detect anti-snipe extension and toast it once
            if (previousClosesAt.HasValue && _auction != null && _auction.ClosesAt > previousClosesAt.Value)
            {
                var extraSeconds = (int)Math.Round((_auction.ClosesAt - previousClosesAt.Value).TotalSeconds);
                ToastService.Show($"Auction extended by {extraSeconds}s (anti-snipe)", ToastType.Warning);
                _suppressUpdateToast = true;
            }
            if (!_suppressUpdateToast)
                ToastService.Show("Bids updated", ToastType.Info);
            _suppressUpdateToast = false;
            StateHasChanged();
        });
    }

    public ValueTask DisposeAsync()
    {
        NotificationService.AuctionUpdated -= OnAuctionUpdated;
        _countdownTimer?.Dispose();
        return ValueTask.CompletedTask;
    }

    private string FormatCountdown(DateTime closesAtUtc)
    {
        var remaining = closesAtUtc - DateTime.UtcNow;
        if (remaining.TotalSeconds <= 0) return "00:00";
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    private string GetCountdownCssClass(DateTime closesAtUtc)
    {
        var seconds = (closesAtUtc - DateTime.UtcNow).TotalSeconds;
        if (seconds <= 0) return "text-danger fw-bold";
        if (seconds < 60) return "text-danger fw-bold";
        if (seconds < 300) return "text-warning fw-bold";
        return "text-success";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _browserUtcOffsetMinutes = await JSRuntime.InvokeAsync<int>("getBrowserUtcOffsetMinutes");
            StateHasChanged();
        }
    }

    private string FormatAuctionTime(DateTime utcTime)
        => utcTime.AddMinutes(-_browserUtcOffsetMinutes).ToString("dd-MM-yyyy HH:mm");

    private async Task LoadData(bool silent = false)
    {
        if (!silent) _isLoading = true;
        _errorMessage = string.Empty;

        try
        {
            // Get current user
            var authState = await AuthenticationStateTask!;
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _errorMessage = "Unable to identify current user.";
                return;
            }

            _isAdmin = authState.User.IsInRole("admin");
            _isOfficer = authState.User.IsInRole("officer");
            _currentUser = await UserRepository.FindByIdAsync(userId);

            // Load auction
            _auction = await AuctionService.GetAuctionByIdAsync(AuctionId);
            if (_auction == null)
            {
                _errorMessage = "Auction not found.";
                return;
            }

            // Load items
            _items = (await AuctionService.GetAuctionItemsAsync(AuctionId)).ToList();

            // Initialize bid amounts and types (only on first load — preserve typed values on silent refresh)
            foreach (var item in _items)
            {
                if (!_bidAmounts.ContainsKey(item.Id)) _bidAmounts[item.Id] = item.MinimumBid;
                if (!_bidTypes.ContainsKey(item.Id)) _bidTypes[item.Id] = "main";
            }

            // Load user's bids
            if (_currentUser != null)
            {
                var userBids = await AuctionService.GetUserBidsForAuctionAsync(userId, AuctionId);
                foreach (var bid in userBids)
                {
                    _userBids[bid.AuctionItemId] = bid;
                    _bidAmounts[bid.AuctionItemId] = bid.Amount;
                    _bidTypes[bid.AuctionItemId] = bid.BidType;
                }

                _totalActiveBids = await AuctionService.GetTotalActiveBidsAsync(userId, AuctionId);
            }

            // Load bids for each item (needed for highest-bid display and the full bid table).
            if (_auction.Status == "closed" || _isAdmin || _auction.Status == "open")
            {
                foreach (var item in _items)
                {
                    var sortedBids = await AuctionService.GetSortedBidsForItemAsync(item.Id);
                    _itemBids[item.Id] = sortedBids;

                    // Load user info for each bidder
                    foreach (var bidWithUser in sortedBids)
                    {
                        if (!_users.ContainsKey(bidWithUser.Bid.UserId))
                        {
                            var user = await UserRepository.FindByIdAsync(bidWithUser.Bid.UserId);
                            if (user != null)
                            {
                                _users[bidWithUser.Bid.UserId] = user;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task PlaceBid(Guid itemId)
    {
        _isProcessing = true;

        try
        {
            if (_currentUser == null) return;

            var amount = _bidAmounts.GetValueOrDefault(itemId, 0);
            var bidType = _bidTypes.GetValueOrDefault(itemId, "main");
            var isUpdate = _userBids.ContainsKey(itemId);

            var result = await AuctionService.PlaceOrUpdateBidAsync(_currentUser.Id, itemId, amount, bidType);

            if (result.Success)
            {
                _suppressUpdateToast = true;
                var msg = isUpdate ? "Bid updated successfully." : "Bid sent successfully.";
                ToastService.Show(msg, ToastType.Success);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Error al enviar la puja.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private int GetFloorForItem(Guid itemId)
    {
        var top = _itemBids.GetValueOrDefault(itemId)?.FirstOrDefault();
        if (top != null) return top.Bid.Amount;
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        return item?.MinimumBid ?? 0;
    }

    private int CalcIncrement(int floor, double pct)
        => floor + (int)Math.Ceiling(floor * pct);

    private int GetAvailableDkpForItem(Guid itemId)
    {
        if (_currentUser == null) return 0;
        var ownBid = _userBids.GetValueOrDefault(itemId)?.Amount ?? 0;
        return _currentUser.DkpBalance - _totalActiveBids + ownBid;
    }

    private bool IsUserWinning(Guid itemId)
    {
        if (_currentUser == null) return false;
        var top = _itemBids.GetValueOrDefault(itemId)?.FirstOrDefault();
        return top != null && top.Bid.UserId == _currentUser.Id;
    }

    private AuctionBid? GetTopOtherBid(Guid itemId)
    {
        if (_currentUser == null) return _itemBids.GetValueOrDefault(itemId)?.FirstOrDefault()?.Bid;
        return _itemBids.GetValueOrDefault(itemId)
            ?.FirstOrDefault(b => b.Bid.UserId != _currentUser.Id)?.Bid;
    }

    private static int BidTypePriority(string bidType) => bidType.ToLower() switch
    {
        "main" => 1,
        "collection" => 2,
        "alt" => 3,
        "greed" => 4,
        _ => 5
    };

    private List<string> GetAllowedBidTypes(Guid itemId)
    {
        var topOther = GetTopOtherBid(itemId);
        var all = new[] { "main", "collection", "alt", "greed" };
        if (topOther == null) return all.ToList();
        var maxPriority = BidTypePriority(topOther.BidType);
        return all.Where(t => BidTypePriority(t) <= maxPriority).ToList();
    }

    private async Task BidPlus15(Guid itemId)
    {
        _bidAmounts[itemId] = CalcIncrement(GetFloorForItem(itemId), 0.15);
        await PlaceBid(itemId);
    }

    private async Task BidPlus30(Guid itemId)
    {
        _bidAmounts[itemId] = CalcIncrement(GetFloorForItem(itemId), 0.30);
        await PlaceBid(itemId);
    }

    private async Task BidAllIn(Guid itemId)
    {
        _bidAmounts[itemId] = GetAvailableDkpForItem(itemId);
        await PlaceBid(itemId);
    }

    private async Task RetractBid(Guid itemId)
    {
        _isProcessing = true;

        try
        {
            if (_currentUser == null) return;

            var result = await AuctionService.RetractBidAsync(_currentUser.Id, itemId);

            if (result.Success)
            {
                _suppressUpdateToast = true;
                ToastService.Show("Bid retracted successfully.", ToastType.Success);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Error al retirar la puja.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task AdminRetractBid(Guid bidId)
    {
        _isProcessing = true;

        try
        {
            var result = await AuctionService.AdminRetractBidAsync(bidId);

            if (result.Success)
            {
                _suppressUpdateToast = true;
                ToastService.Show("Bid retried by administrator.", ToastType.Success);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Error al retirar la puja.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task StartAuction()
    {
        _isProcessing = true;

        try
        {
            var result = await AuctionService.StartAuctionAsync(AuctionId);

            if (result.Success)
            {
                ToastService.Show("Auction started successfully", ToastType.Success);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Error al iniciar la subasta.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task CloseAuction()
    {
        _isProcessing = true;

        try
        {
            var result = await AuctionService.CloseAuctionAsync(AuctionId);

            if (result.Success)
            {
                ToastService.Show("Auction closed successfully.", ToastType.Success);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Error al cerrar la subasta.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task CancelAuction()
    {
        _isProcessing = true;

        try
        {
            var result = await AuctionService.CancelAuctionAsync(AuctionId);

            if (result.Success)
            {
                ToastService.Show("Auction cancelled.", ToastType.Warning);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Error al cancelar la subasta.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task DeliverItem(Guid itemId, Guid winnerId, int finalPrice)
    {
        _isProcessing = true;

        try
        {
            if (_currentUser == null) return;

            var result = await AuctionService.DeliverItemAsync(itemId, winnerId, finalPrice, _currentUser.Id);

            if (result.Success)
            {
                ToastService.Show("Item delivered successfully", ToastType.Success);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Failed to deliver item. Please try again.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task SkipItem(Guid itemId)
    {
        _isProcessing = true;

        try
        {
            if (_currentUser == null) return;

            var result = await AuctionService.SkipItemAsync(itemId, _currentUser.Id);

            if (result.Success)
            {
                ToastService.Show("Item skipped.", ToastType.Info);
                await LoadData();
            }
            else
            {
                ToastService.Show(result.ErrorMessage ?? "Failed to skip item. Please try again.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private string GetBidTypeBadgeClass(string bidType)
    {
        return bidType.ToLower() switch
        {
            "main" => "bg-primary",
            "collection" => "bg-success",
            "alt" => "bg-info",
            "greed" => "bg-secondary",
            _ => "bg-warning"
        };
    }

    private string GetBidTypeLabel(string bidType)
    {
        return bidType.ToLower() switch
        {
            "main" => "PVP",
            "alt" => "ALT/ZEN",
            "collection" => "COLLECTION",
            "greed" => "GREED",
            _ => bidType.ToUpper()
        };
    }

    private string GenerateSummaryText()
    {
        var sb = new System.Text.StringBuilder();

        var pendingEntries = _items
            .Where(i => !i.Delivered)
            .Select(i => (Item: i, TopBid: _itemBids.GetValueOrDefault(i.Id)?.FirstOrDefault()))
            .Where(x => x.TopBid != null)
            .ToList();

        var deliveredItems = _items.Where(i => i.Delivered && i.WinnerId.HasValue).ToList();

        int totalAuctioned = _items.Count;
        int totalSold = pendingEntries.Count + deliveredItems.Count;
        int totalDkp = pendingEntries.Sum(x => x.TopBid!.Bid.Amount)
                     + deliveredItems.Sum(i => i.FinalPrice ?? 0);

        sb.AppendLine($"=== RESUMEN: {_auction!.Name} ===");
        sb.AppendLine($"Items subastados : {totalAuctioned}");
        sb.AppendLine($"Items comprados  : {totalSold}");
        sb.AppendLine($"DKP gastados     : {totalDkp}");

        // Build unified list of (winnerId, winnerName, itemName, dkp, delivered)
        var allWinnerItems = pendingEntries
            .Select(x => (
                WinnerId: x.TopBid!.Bid.UserId,
                WinnerName: _users.GetValueOrDefault(x.TopBid!.Bid.UserId)?.Username ?? "?",
                ItemName: x.Item.Name,
                Dkp: x.TopBid!.Bid.Amount,
                Delivered: false
            ))
            .Concat(deliveredItems.Select(i => (
                WinnerId: i.WinnerId!.Value,
                WinnerName: _users.GetValueOrDefault(i.WinnerId!.Value)?.Username ?? "?",
                ItemName: i.Name,
                Dkp: i.FinalPrice ?? 0,
                Delivered: true
            )))
            .GroupBy(x => (x.WinnerId, x.WinnerName))
            .OrderBy(g => g.Key.WinnerName)
            .ToList();

        if (allWinnerItems.Any())
        {
            sb.AppendLine("\nITEMS A ENTREGAR:");
            foreach (var group in allWinnerItems)
            {
                sb.AppendLine($"\n  {group.Key.WinnerName}:");
                foreach (var entry in group)
                {
                    var status = entry.Delivered ? "✓" : "•";
                    sb.AppendLine($"    {status} {entry.ItemName} ({entry.Dkp} DKP)");
                }
            }
        }

        return sb.ToString();
    }

    private async Task CopyToClipboard()
    {
        var text = GenerateSummaryText();
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        _copiedToClipboard = true;
        StateHasChanged();
        await Task.Delay(2000);
        _copiedToClipboard = false;
        StateHasChanged();
    }
}
