using Microsoft.AspNetCore.Components;
using DkpSystem.Models;
using DkpSystem.Models.ViewModels;
using DkpSystem.Services;

namespace DkpSystem.Components.Pages.Admin.Events;

public partial class EventEdit : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public Guid EventId { get; set; }

    private Event? eventModel;
    private EventEditModel model = new();
    private bool isLoading = true;
    private bool isSubmitting = false;
    private bool isDeleting = false;
    private bool showDeleteModal = false;
    private bool hasEarnings = false;
    private string? errorMessage;
    private string? successMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadEventAsync();
        isLoading = false;
    }

    private async Task LoadEventAsync()
    {
        eventModel = await EventService.GetEventByIdAsync(EventId);

        if (eventModel != null)
        {
            model.Name = eventModel.Name;
            model.Description = eventModel.Description;

            // Check if event has earnings
            var summary = (await EventService.GetEventSummariesAsync(eventModel.GuildId))
                .FirstOrDefault(e => e.Id == EventId);
            hasEarnings = summary?.TotalDkpDistributed > 0;
        }
    }

    private async Task HandleSubmitAsync()
    {
        errorMessage = null;
        successMessage = null;
        isSubmitting = true;

        try
        {
            var success = await EventService.UpdateEventAsync(EventId, model.Name, model.Description);

            if (success)
            {
                successMessage = "Event updated successfully!";
                await LoadEventAsync();
            }
            else
            {
                errorMessage = "Failed to update event.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error updating event: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void ShowDeleteConfirmation()
    {
        showDeleteModal = true;
    }

    private void HideDeleteConfirmation()
    {
        showDeleteModal = false;
    }

    private async Task HandleDeleteAsync()
    {
        errorMessage = null;
        isDeleting = true;

        try
        {
            var success = await EventService.DeleteEventAsync(EventId);

            if (success)
            {
                Navigation.NavigateTo("/events");
            }
            else
            {
                errorMessage = "Cannot delete event: This event has associated DKP earnings. Historical records must be preserved.";
                HideDeleteConfirmation();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting event: {ex.Message}";
            HideDeleteConfirmation();
        }
        finally
        {
            isDeleting = false;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo($"/events/{EventId}");
    }
}
