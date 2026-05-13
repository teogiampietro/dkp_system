using DkpSystem.Data.Repositories;
using DkpSystem.Models;

namespace DkpSystem.Services;

/// <inheritdoc cref="IAttendanceService"/>
public class AttendanceService : IAttendanceService
{
    private readonly AttendanceRepository _attendanceRepository;
    private readonly EventRepository _eventRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttendanceService"/> class.
    /// </summary>
    public AttendanceService(AttendanceRepository attendanceRepository, EventRepository eventRepository)
    {
        _attendanceRepository = attendanceRepository;
        _eventRepository = eventRepository;
    }

    /// <inheritdoc />
    public async Task<AttendanceMatrix> GetAttendanceMatrixAsync(Guid guildId, DateTime? from, DateTime? to, int limit)
    {
        if (limit <= 0)
        {
            limit = 20;
        }

        var eventsTask = _attendanceRepository.GetRecentEventsAsync(guildId, from, to, limit);
        var usersTask = _eventRepository.GetActiveGuildMembersAsync(guildId);

        await Task.WhenAll(eventsTask, usersTask);

        var events = (await eventsTask).ToList();
        var users = (await usersTask).ToList();

        var attendancePairs = await _attendanceRepository.GetAttendanceAsync(events.Select(e => e.Id));

        return new AttendanceMatrix
        {
            Events = events,
            Users = users,
            Attendance = attendancePairs.ToHashSet()
        };
    }
}
