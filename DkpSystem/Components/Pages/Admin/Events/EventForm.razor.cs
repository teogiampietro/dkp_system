using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using DkpSystem.Models;
using DkpSystem.Models.ViewModels;
using DkpSystem.Services;

namespace DkpSystem.Components.Pages.Admin.Events;

public partial class EventForm : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private EventFormModel model = new();
    private IEnumerable<User>? guildMembers;
    private HashSet<Guid> selectedAttendees = new();
    private bool isLoadingMembers = true;
    private bool isSubmitting = false;
    private string? errorMessage;
    private string? successMessage;
    private Guid currentUserId;
    private Guid currentGuildId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                currentUserId = userId;
            }

            var guildIdClaim = user.FindFirst("GuildId");
            if (guildIdClaim != null && Guid.TryParse(guildIdClaim.Value, out var guildId) && guildId != Guid.Empty)
            {
                currentGuildId = guildId;
                await LoadGuildMembersAsync();
            }
            else
            {
                // If user doesn't have a guild assigned, get all active members from all guilds
                // This is a temporary workaround - in production, all users should have a guild_id
                await LoadAllActiveMembersAsync();
            }
        }

        isLoadingMembers = false;
    }

    private async Task LoadGuildMembersAsync()
    {
        if (currentGuildId == Guid.Empty)
        {
            await LoadAllActiveMembersAsync();
            return;
        }

        guildMembers = await EventService.GetActiveGuildMembersAsync(currentGuildId);

        // Pre-select all members by default
        selectedAttendees = guildMembers.Select(m => m.Id).ToHashSet();
    }

    private async Task LoadAllActiveMembersAsync()
    {
        guildMembers = await EventService.GetAllActiveMembersAsync();

        // Pre-select all members by default
        selectedAttendees = guildMembers.Select(m => m.Id).ToHashSet();

        // Use the first guild found, or create a default one
        var firstMember = guildMembers.FirstOrDefault();
        if (firstMember != null && firstMember.GuildId.HasValue && firstMember.GuildId.Value != Guid.Empty)
        {
            currentGuildId = firstMember.GuildId.Value;
        }
    }

    private void ToggleAttendee(Guid userId, bool isChecked)
    {
        if (isChecked)
        {
            selectedAttendees.Add(userId);
        }
        else
        {
            selectedAttendees.Remove(userId);
        }
    }

    private void SelectAll()
    {
        if (guildMembers != null)
        {
            selectedAttendees = guildMembers.Select(m => m.Id).ToHashSet();
        }
    }

    private void DeselectAll()
    {
        selectedAttendees.Clear();
    }

    private async Task HandleSubmitAsync()
    {
        errorMessage = null;
        successMessage = null;
        isSubmitting = true;

        try
        {
            if (!selectedAttendees.Any())
            {
                errorMessage = "Please select at least one attendee.";
                isSubmitting = false;
                return;
            }

            var createdEvent = await EventService.CreateEventAsync(
                model.Name,
                model.Description,
                currentGuildId,
                currentUserId,
                selectedAttendees
            );

            successMessage = "Event created successfully! Redirecting to event detail...";
            await Task.Delay(1500);
            Navigation.NavigateTo($"/events/{createdEvent.Id}");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error creating event: {ex.Message}";
            isSubmitting = false;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/events");
    }
}
