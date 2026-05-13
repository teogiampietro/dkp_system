using System.ComponentModel.DataAnnotations;
using DkpSystem.Models;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Pages.Events;

public partial class EventDetail : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [Parameter]
    public Guid EventId { get; set; }

    private Event? eventModel;
    private IEnumerable<AwardHistoryEntry>? awardHistory;
    private IEnumerable<User>? confirmedAttendees;
    private IEnumerable<User>? availableAttendees;
    private IEnumerable<RaiderEarningEntry>? raiderEarnings;
    private bool isLoading = true;
    private bool isSubmittingGroupAward = false;
    private bool isSubmittingIndividualAward = false;
    private string? errorMessage;
    private string? successMessage;
    private Guid currentUserId;
    private bool isAdmin = false;

    private GroupAwardFormModel groupAwardModel = new();
    private IndividualAwardFormModel individualAwardModel = new();

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

            isAdmin = user.IsInRole("admin") || user.IsInRole("officer");
        }

        await LoadEventDataAsync();
        isLoading = false;
    }

    private async Task LoadEventDataAsync()
    {
        eventModel = await EventService.GetEventByIdAsync(EventId);

        if (eventModel == null)
        {
            return;
        }

        if (isAdmin)
        {
            awardHistory = await EventService.GetAwardHistoryAsync(EventId);
            confirmedAttendees = await EventService.GetConfirmedAttendeesAsync(EventId);
            availableAttendees = await EventService.GetEventAttendeesAsync(EventId, eventModel.GuildId);
            groupAwardModel.SelectedAttendees = availableAttendees.Select(a => a.Id).ToHashSet();
        }
        else
        {
            raiderEarnings = await EventService.GetRaiderEarningsAsync(EventId, currentUserId);
        }
    }

    private void ToggleGroupAttendee(Guid userId, bool isChecked)
    {
        if (isChecked)
        {
            groupAwardModel.SelectedAttendees.Add(userId);
        }
        else
        {
            groupAwardModel.SelectedAttendees.Remove(userId);
        }
    }

    private void ToggleAllGroupAttendees(bool isChecked)
    {
        if (availableAttendees == null) return;

        if (isChecked)
        {
            groupAwardModel.SelectedAttendees = availableAttendees.Select(a => a.Id).ToHashSet();
        }
        else
        {
            groupAwardModel.SelectedAttendees.Clear();
        }
    }

    private async Task HandleGroupAwardAsync()
    {
        errorMessage = null;
        successMessage = null;
        isSubmittingGroupAward = true;

        try
        {
            if (!groupAwardModel.SelectedAttendees.Any())
            {
                errorMessage = "Please select at least one attendee.";
                isSubmittingGroupAward = false;
                return;
            }

            await EventService.AddGroupAwardAsync(
                EventId,
                groupAwardModel.Reason,
                groupAwardModel.DkpAmount,
                groupAwardModel.SelectedAttendees
            );

            successMessage = $"Group award of {groupAwardModel.DkpAmount} DKP added successfully to {groupAwardModel.SelectedAttendees.Count} raiders!";

            // Reset form
            groupAwardModel = new GroupAwardFormModel
            {
                SelectedAttendees = availableAttendees?.Select(a => a.Id).ToHashSet() ?? new HashSet<Guid>()
            };

            // Reload data
            await LoadEventDataAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error adding group award: {ex.Message}";
        }
        finally
        {
            isSubmittingGroupAward = false;
        }
    }

    private async Task HandleIndividualAwardAsync()
    {
        errorMessage = null;
        successMessage = null;
        isSubmittingIndividualAward = true;

        try
        {
            await EventService.AddIndividualAwardAsync(
                EventId,
                Guid.Parse(individualAwardModel.UserId),
                individualAwardModel.Reason,
                individualAwardModel.DkpAmount
            );

            var raiderName = availableAttendees?.FirstOrDefault(a => a.Id == Guid.Parse(individualAwardModel.UserId))?.Username ?? "Raider";
            successMessage = $"Individual award of {individualAwardModel.DkpAmount} DKP added successfully to {raiderName}!";

            // Reset form
            individualAwardModel = new IndividualAwardFormModel();

            // Reload data
            await LoadEventDataAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error adding individual award: {ex.Message}";
        }
        finally
        {
            isSubmittingIndividualAward = false;
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/events");
    }

    private class GroupAwardFormModel
    {
        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters.")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "DKP amount is required.")]
        [Range(1, 10000, ErrorMessage = "DKP amount must be between 1 and 10000.")]
        public int DkpAmount { get; set; } = 20;

        public HashSet<Guid> SelectedAttendees { get; set; } = new();
    }

    private class IndividualAwardFormModel
    {
        [Required(ErrorMessage = "Please select a raider.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters.")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "DKP amount is required.")]
        [Range(1, 10000, ErrorMessage = "DKP amount must be between 1 and 10000.")]
        public int DkpAmount { get; set; } = 10;
    }
}
