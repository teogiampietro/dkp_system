using DkpSystem.Data.Repositories;
using DkpSystem.Models;

namespace DkpSystem.Services;

/// <summary>
/// Service for managing events and DKP awards.
/// </summary>
public class EventService
{
    private readonly EventRepository _eventRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventService"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository.</param>
    public EventService(EventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    /// <summary>
    /// Creates a new event with confirmed attendees.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="description">The event description.</param>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="createdBy">The user ID who created the event.</param>
    /// <param name="attendeeIds">The list of confirmed attendee user IDs.</param>
    /// <returns>The created event.</returns>
    public async Task<Event> CreateEventAsync(string name, string? description, Guid guildId, Guid createdBy, IEnumerable<Guid> attendeeIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name is required.", nameof(name));
        }

        var eventModel = new Event
        {
            Name = name,
            Description = description,
            GuildId = guildId,
            CreatedBy = createdBy
        };

        var createdEvent = await _eventRepository.CreateAsync(eventModel);
        
        // Store attendees for later use when awards are created
        await _eventRepository.SaveAttendeesAsync(createdEvent.Id, attendeeIds);

        return createdEvent;
    }

    /// <summary>
    /// Gets all events for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of events with summary information.</returns>
    public async Task<IEnumerable<EventSummary>> GetEventSummariesAsync(Guid guildId)
    {
        var events = await _eventRepository.GetByGuildIdAsync(guildId);
        var summaries = new List<EventSummary>();

        foreach (var evt in events)
        {
            var totalDkp = await _eventRepository.GetTotalDkpDistributedAsync(evt.Id);
            var attendeeCount = await _eventRepository.GetAttendeeCountAsync(evt.Id);

            summaries.Add(new EventSummary
            {
                Id = evt.Id,
                Name = evt.Name,
                CreatedAt = evt.CreatedAt,
                TotalDkpDistributed = totalDkp,
                AttendeeCount = attendeeCount
            });
        }

        return summaries;
    }

    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    public async Task<Event?> GetEventByIdAsync(Guid eventId)
    {
        return await _eventRepository.GetByIdAsync(eventId);
    }

    /// <summary>
    /// Updates an event's name and description.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    public async Task<bool> UpdateEventAsync(Guid eventId, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name is required.", nameof(name));
        }

        return await _eventRepository.UpdateAsync(eventId, name, description);
    }

    /// <summary>
    /// Deletes an event if it has no associated earnings.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>True if the event was deleted; false if it has earnings.</returns>
    public async Task<bool> DeleteEventAsync(Guid eventId)
    {
        return await _eventRepository.DeleteAsync(eventId);
    }

    /// <summary>
    /// Adds a group award to all confirmed attendees of an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="reason">The reason for the award.</param>
    /// <param name="dkpAmount">The DKP amount to award.</param>
    /// <param name="attendeeIds">The list of attendee user IDs.</param>
    /// <returns>The number of earnings created.</returns>
    public async Task<int> AddGroupAwardAsync(Guid eventId, string reason, int dkpAmount, IEnumerable<Guid> attendeeIds)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        if (dkpAmount <= 0)
        {
            throw new ArgumentException("DKP amount must be greater than zero.", nameof(dkpAmount));
        }

        var attendeeList = attendeeIds.ToList();
        if (!attendeeList.Any())
        {
            throw new ArgumentException("At least one attendee is required.", nameof(attendeeIds));
        }

        // Create reward line
        var rewardLine = new EventRewardLine
        {
            EventId = eventId,
            Reason = reason,
            DkpAmount = dkpAmount
        };

        var createdRewardLine = await _eventRepository.CreateRewardLineAsync(rewardLine);

        // Create earnings for all attendees
        var earnings = attendeeList.Select(userId => new DkpEarning
        {
            UserId = userId,
            EventId = eventId,
            RewardLineId = createdRewardLine.Id,
            DkpAmount = dkpAmount
        });

        return await _eventRepository.CreateEarningsAsync(earnings);
    }

    /// <summary>
    /// Adds an individual award to a specific raider.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID to award.</param>
    /// <param name="reason">The reason for the award.</param>
    /// <param name="dkpAmount">The DKP amount to award.</param>
    /// <returns>The number of earnings created (should be 1).</returns>
    public async Task<int> AddIndividualAwardAsync(Guid eventId, Guid userId, string reason, int dkpAmount)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        if (dkpAmount <= 0)
        {
            throw new ArgumentException("DKP amount must be greater than zero.", nameof(dkpAmount));
        }

        // Create reward line
        var rewardLine = new EventRewardLine
        {
            EventId = eventId,
            Reason = reason,
            DkpAmount = dkpAmount
        };

        var createdRewardLine = await _eventRepository.CreateRewardLineAsync(rewardLine);

        // Create earning for the specific user
        var earning = new DkpEarning
        {
            UserId = userId,
            EventId = eventId,
            RewardLineId = createdRewardLine.Id,
            DkpAmount = dkpAmount
        };

        return await _eventRepository.CreateEarningsAsync(new[] { earning });
    }

    /// <summary>
    /// Gets the full award history for an event (admin view).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of award history entries.</returns>
    public async Task<IEnumerable<AwardHistoryEntry>> GetAwardHistoryAsync(Guid eventId)
    {
        var rewardLines = await _eventRepository.GetRewardLinesAsync(eventId);
        var earnings = await _eventRepository.GetEarningsAsync(eventId);
        
        var history = new List<AwardHistoryEntry>();

        foreach (var rewardLine in rewardLines)
        {
            var lineEarnings = earnings.Where(e => e.RewardLineId == rewardLine.Id).ToList();
            var isGroupAward = lineEarnings.Count > 1;

            history.Add(new AwardHistoryEntry
            {
                RewardLineId = rewardLine.Id,
                Reason = rewardLine.Reason,
                DkpAmount = rewardLine.DkpAmount,
                IsGroupAward = isGroupAward,
                RecipientCount = lineEarnings.Count,
                CreatedAt = lineEarnings.FirstOrDefault()?.EarnedAt ?? DateTime.UtcNow
            });
        }

        return history.OrderByDescending(h => h.CreatedAt);
    }

    /// <summary>
    /// Gets a raider's earnings for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of the raider's earnings with reasons.</returns>
    public async Task<IEnumerable<RaiderEarningEntry>> GetRaiderEarningsAsync(Guid eventId, Guid userId)
    {
        var earnings = await _eventRepository.GetUserEarningsAsync(eventId, userId);
        var rewardLines = await _eventRepository.GetRewardLinesAsync(eventId);
        
        var entries = new List<RaiderEarningEntry>();

        foreach (var earning in earnings)
        {
            var rewardLine = rewardLines.FirstOrDefault(rl => rl.Id == earning.RewardLineId);
            if (rewardLine != null)
            {
                entries.Add(new RaiderEarningEntry
                {
                    Reason = rewardLine.Reason,
                    DkpAmount = earning.DkpAmount,
                    EarnedAt = earning.EarnedAt
                });
            }
        }

        return entries.OrderByDescending(e => e.EarnedAt);
    }

    /// <summary>
    /// Gets all active guild members for attendance selection.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A list of active users in the guild.</returns>
    public async Task<IEnumerable<User>> GetActiveGuildMembersAsync(Guid guildId)
    {
        return await _eventRepository.GetActiveGuildMembersAsync(guildId);
    }

    /// <summary>
    /// Gets all active members regardless of guild assignment.
    /// </summary>
    /// <returns>A list of all active users.</returns>
    public async Task<IEnumerable<User>> GetAllActiveMembersAsync()
    {
        return await _eventRepository.GetAllActiveMembersAsync();
    }

    /// <summary>
    /// Gets the first available guild ID from active members.
    /// </summary>
    /// <returns>The first guild ID found, or Guid.Empty if none.</returns>
    public async Task<Guid> GetFirstAvailableGuildIdAsync()
    {
        var members = await _eventRepository.GetAllActiveMembersAsync();
        var firstMemberWithGuild = members.FirstOrDefault(m => m.GuildId.HasValue && m.GuildId.Value != Guid.Empty);
        return firstMemberWithGuild?.GuildId ?? Guid.Empty;
    }

    /// <summary>
    /// Gets confirmed attendees for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of users who attended the event.</returns>
    public async Task<IEnumerable<User>> GetConfirmedAttendeesAsync(Guid eventId)
    {
        return await _eventRepository.GetConfirmedAttendeesAsync(eventId);
    }

    /// <summary>
    /// Gets the pre-confirmed attendees recorded at event creation time.
    /// Falls back to all active guild members for events created before attendee tracking.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="guildId">The guild ID used as fallback.</param>
    /// <returns>A list of attendees for the event.</returns>
    public async Task<IEnumerable<User>> GetEventAttendeesAsync(Guid eventId, Guid guildId)
    {
        return await _eventRepository.GetEventAttendeesAsync(eventId, guildId);
    }
}

/// <summary>
/// Summary information for an event in the event list.
/// </summary>
public class EventSummary
{
    /// <summary>
    /// Gets or sets the event ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the total DKP distributed.
    /// </summary>
    public int TotalDkpDistributed { get; set; }

    /// <summary>
    /// Gets or sets the number of confirmed attendees.
    /// </summary>
    public int AttendeeCount { get; set; }
}

/// <summary>
/// Award history entry for admin view.
/// </summary>
public class AwardHistoryEntry
{
    /// <summary>
    /// Gets or sets the reward line ID.
    /// </summary>
    public Guid RewardLineId { get; set; }

    /// <summary>
    /// Gets or sets the reason for the award.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount.
    /// </summary>
    public int DkpAmount { get; set; }

    /// <summary>
    /// Gets or sets whether this is a group award.
    /// </summary>
    public bool IsGroupAward { get; set; }

    /// <summary>
    /// Gets or sets the number of recipients.
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Raider earning entry for raider view.
/// </summary>
public class RaiderEarningEntry
{
    /// <summary>
    /// Gets or sets the reason for the earning.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount earned.
    /// </summary>
    public int DkpAmount { get; set; }

    /// <summary>
    /// Gets or sets the date earned.
    /// </summary>
    public DateTime EarnedAt { get; set; }
}
