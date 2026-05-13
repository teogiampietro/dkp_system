namespace DkpSystem.Models;

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
