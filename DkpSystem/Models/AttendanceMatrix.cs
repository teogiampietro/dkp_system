namespace DkpSystem.Models;

/// <summary>
/// Represents an attendance matrix for a guild: a list of users (rows),
/// a list of events (columns), and the set of (userId, eventId) pairs that attended.
/// </summary>
public class AttendanceMatrix
{
    /// <summary>
    /// Gets or sets the active users included in the matrix, ordered by username.
    /// </summary>
    public List<User> Users { get; set; } = [];

    /// <summary>
    /// Gets or sets the events included in the matrix, ordered by date descending.
    /// </summary>
    public List<Event> Events { get; set; } = [];

    /// <summary>
    /// Gets or sets the set of attendance pairs (userId, eventId).
    /// </summary>
    public HashSet<(Guid UserId, Guid EventId)> Attendance { get; set; } = [];

    /// <summary>
    /// Returns true if the given user has an earning recorded for the given event.
    /// </summary>
    public bool Attended(Guid userId, Guid eventId) => Attendance.Contains((userId, eventId));
}
