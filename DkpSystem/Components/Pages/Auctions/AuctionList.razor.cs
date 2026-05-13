using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using DkpSystem.Models;
using DkpSystem.Services;
using DkpSystem.Data.Repositories;

namespace DkpSystem.Components.Pages.Auctions;

public partial class AuctionList : ComponentBase
{
    [Inject] private IAuctionService AuctionService { get; set; } = default!;
    [Inject] private AuctionRepository AuctionRepository { get; set; } = default!;
    [Inject] private UserRepository UserRepository { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private List<Auction> _openAuctions = new();
    private List<Auction> _pendingAuctions = new();
    private List<Auction> _closedAuctions = new();
    private Dictionary<Guid, int> _auctionItemCounts = new();
    private Dictionary<Guid, int> _auctionBidCounts = new();
    private bool _isLoading = true;
    private string _errorMessage = string.Empty;
    private int _browserUtcOffsetMinutes = 0;

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

    protected override async Task OnInitializedAsync()
    {
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

            // Get user's guild
            var user = await UserRepository.FindByIdAsync(userId);
            if (user == null || user.GuildId == null)
            {
                _errorMessage = "You must be assigned to a guild to view auctions.";
                return;
            }

            // Load all auctions
            var allAuctions = await AuctionService.GetAuctionsByGuildAsync(user.GuildId.Value);

            _openAuctions = allAuctions.Where(a => a.Status == "open").OrderByDescending(a => a.CreatedAt).ToList();
            _pendingAuctions = allAuctions.Where(a => a.Status == "pending").OrderByDescending(a => a.CreatedAt).ToList();
            _closedAuctions = allAuctions.Where(a => a.Status == "closed").OrderByDescending(a => a.ClosedAt).ToList();

            // Load item and bid counts for all auctions
            foreach (var auction in allAuctions)
            {
                _auctionItemCounts[auction.Id] = await AuctionRepository.GetAuctionItemCountAsync(auction.Id);
                _auctionBidCounts[auction.Id] = await AuctionRepository.GetAuctionBidCountAsync(auction.Id);
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
}
