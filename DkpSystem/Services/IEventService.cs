using DkpSystem.Models;

namespace DkpSystem.Services;

/// <summary>
/// Service for managing events and DKP awards.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Creates a new event with confirmed attendees.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="description">The event description.</param>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="createdBy">The user ID who created the event.</param>
    /// <param name="attendeeIds">The list of confirmed attendee user IDs.</param>
    /// <returns>The created event.</returns>
    Task<Event> CreateEventAsync(string name, string? description, Guid guildId, Guid createdBy, IEnumerable<Guid> attendeeIds);

    /// <summary>
    /// Gets all events for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of events with summary information.</returns>
    Task<IEnumerable<EventSummary>> GetEventSummariesAsync(Guid guildId);

    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    Task<Event?> GetEventByIdAsync(Guid eventId);

    /// <summary>
    /// Updates an event's name and description.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateEventAsync(Guid eventId, string name, string? description);

    /// <summary>
    /// Deletes an event if it has no associated earnings.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>True if the event was deleted; false if it has earnings.</returns>
    Task<bool> DeleteEventAsync(Guid eventId);

    /// <summary>
    /// Adds a group award to all confirmed attendees of an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="reason">The reason for the award.</param>
    /// <param name="dkpAmount">The DKP amount to award.</param>
    /// <param name="attendeeIds">The list of attendee user IDs.</param>
    /// <returns>The number of earnings created.</returns>
    Task<int> AddGroupAwardAsync(Guid eventId, string reason, int dkpAmount, IEnumerable<Guid> attendeeIds);

    /// <summary>
    /// Adds an individual award to a specific raider.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID to award.</param>
    /// <param name="reason">The reason for the award.</param>
    /// <param name="dkpAmount">The DKP amount to award.</param>
    /// <returns>The number of earnings created (should be 1).</returns>
    Task<int> AddIndividualAwardAsync(Guid eventId, Guid userId, string reason, int dkpAmount);

    /// <summary>
    /// Gets the full award history for an event (admin view).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of award history entries.</returns>
    Task<IEnumerable<AwardHistoryEntry>> GetAwardHistoryAsync(Guid eventId);

    /// <summary>
    /// Gets a raider's earnings for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of the raider's earnings with reasons.</returns>
    Task<IEnumerable<RaiderEarningEntry>> GetRaiderEarningsAsync(Guid eventId, Guid userId);

    /// <summary>
    /// Gets all active guild members for attendance selection.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of active users in the guild.</returns>
    Task<IEnumerable<User>> GetActiveGuildMembersAsync(Guid guildId);

    /// <summary>
    /// Gets all active members regardless of guild assignment.
    /// </summary>
    /// <returns>A list of all active users.</returns>
    Task<IEnumerable<User>> GetAllActiveMembersAsync();

    /// <summary>
    /// Gets the first available guild ID from active members.
    /// </summary>
    /// <returns>The first guild ID found, or Guid.Empty if none.</returns>
    Task<Guid> GetFirstAvailableGuildIdAsync();

    /// <summary>
    /// Gets confirmed attendees for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of users who attended the event.</returns>
    Task<IEnumerable<User>> GetConfirmedAttendeesAsync(Guid eventId);

    /// <summary>
    /// Gets the pre-confirmed attendees recorded at event creation time.
    /// Falls back to all active guild members for events created before attendee tracking.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="guildId">The guild ID used as fallback.</param>
    /// <returns>A list of attendees for the event.</returns>
    Task<IEnumerable<User>> GetEventAttendeesAsync(Guid eventId, Guid guildId);
}
