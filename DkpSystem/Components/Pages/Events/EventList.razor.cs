using DkpSystem.Models;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Pages.Events;

public partial class EventList : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private IEnumerable<EventSummary>? events;
    private bool isLoading = true;
    private Guid currentGuildId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var guildIdClaim = user.FindFirst("GuildId");
            if (guildIdClaim != null && Guid.TryParse(guildIdClaim.Value, out var guildId) && guildId != Guid.Empty)
            {
                currentGuildId = guildId;
            }
            else
            {
                // If user doesn't have a guild, get the first available guild
                currentGuildId = await EventService.GetFirstAvailableGuildIdAsync();
            }

            if (currentGuildId != Guid.Empty)
            {
                await LoadEventsAsync();
            }
        }

        isLoading = false;
    }

    private async Task LoadEventsAsync()
    {
        events = await EventService.GetEventSummariesAsync(currentGuildId);
    }
}
