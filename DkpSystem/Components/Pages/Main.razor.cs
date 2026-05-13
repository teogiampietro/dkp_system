using System.Security.Claims;
using DkpSystem.Models;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Pages;

public partial class Main : ComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = default!;
    [Inject] private IAuctionService AuctionService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private int? _memberCount;
    private int? _totalDkpInCirculation;
    private int? _activeAuctions;
    private string? _yourRankLabel;
    private int? _userBalance;
    private List<RecentDeliveredItem> _recentItems = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var members = (await MemberService.GetAllMembersAsync()).ToList();
            _memberCount = members.Count;
            _totalDkpInCirculation = members.Sum(m => m.DkpBalance);

            Guid? userId = null;
            User? currentUser = null;
            if (AuthenticationStateTask is not null)
            {
                var authState = await AuthenticationStateTask;
                var claim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(claim, out var parsed))
                {
                    userId = parsed;
                    currentUser = await MemberService.GetMemberByIdAsync(parsed);
                    _userBalance = currentUser?.DkpBalance;
                }
            }

            if (currentUser?.GuildId is Guid guildId)
            {
                var openAuctions = await AuctionService.GetOpenAuctionsByGuildAsync(guildId);
                _activeAuctions = openAuctions.Count();

                var ranking = (await MemberService.GetMemberRankingByGuildAsync(guildId)).ToList();
                var rank = ranking.FindIndex(u => u.Id == userId);
                if (rank >= 0)
                {
                    _yourRankLabel = $"#{rank + 1}";
                }

                _recentItems = (await AuctionService.GetRecentDeliveredItemsAsync(guildId, 5)).ToList();
            }
        }
        catch
        {
            // Render the page with whatever data loaded; missing stats show as "—".
        }
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string FormatRelative(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return utc.ToString("MMM d");
    }
}
