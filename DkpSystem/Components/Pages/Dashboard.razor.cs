using System.Security.Claims;
using DkpSystem.Models;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] private IAuctionService AuctionService { get; set; } = default!;
    [Inject] private IMemberService MemberService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private bool _isLoading = true;
    private string? _errorMessage;
    private Guid _currentUserId;
    private Guid? _guildId;
    private List<RecentDeliveredItem> _recentItems = [];
    private List<Auction> _openAuctions = [];
    private List<User> _ranking = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthenticationStateTask!;
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _errorMessage = "Could not identify the current user.";
                return;
            }

            _currentUserId = userId;

            var currentUser = await MemberService.GetMemberByIdAsync(userId);
            if (currentUser?.GuildId == null)
            {
                _errorMessage = "You are not assigned to a guild. Please contact an admin.";
                return;
            }

            _guildId = currentUser.GuildId;

            var recentTask = AuctionService.GetRecentDeliveredItemsAsync(_guildId.Value);
            var openTask = AuctionService.GetOpenAuctionsByGuildAsync(_guildId.Value);
            var rankingTask = MemberService.GetMemberRankingByGuildAsync(_guildId.Value);

            await Task.WhenAll(recentTask, openTask, rankingTask);

            _recentItems = (await recentTask).ToList();
            _openAuctions = (await openTask).ToList();
            _ranking = (await rankingTask).ToList();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }
}
