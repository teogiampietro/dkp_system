using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using DkpSystem.Models;
using DkpSystem.Services;
using DkpSystem.Data.Repositories;

namespace DkpSystem.Components.Pages.Auctions;

public partial class AuctionHistory : ComponentBase
{
    [Inject] private IAuctionService AuctionService { get; set; } = default!;
    [Inject] private AuctionRepository AuctionRepository { get; set; } = default!;
    [Inject] private UserRepository UserRepository { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private List<Auction> _closedAuctions = new();
    private Dictionary<Guid, int> _auctionItemCounts = new();
    private Dictionary<Guid, int> _auctionBidCounts = new();
    private bool _isLoading = true;
    private string _errorMessage = string.Empty;

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
                _errorMessage = "You must be assigned to a guild to view auction history.";
                return;
            }

            // Load closed auctions
            _closedAuctions = (await AuctionService.GetClosedAuctionsAsync(user.GuildId.Value))
                .OrderByDescending(a => a.ClosedAt)
                .ToList();

            // Load item and bid counts
            foreach (var auction in _closedAuctions)
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
