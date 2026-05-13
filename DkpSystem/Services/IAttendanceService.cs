using DkpSystem.Models;

namespace DkpSystem.Services;

/// <summary>
/// Service that builds the attendance matrix for a guild.
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Builds the attendance matrix for a guild.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="from">Optional inclusive lower bound on event date.</param>
    /// <param name="to">Optional inclusive upper bound on event date.</param>
    /// <param name="limit">Maximum number of events (columns) to return.</param>
    /// <returns>The attendance matrix.</returns>
    Task<AttendanceMatrix> GetAttendanceMatrixAsync(Guid guildId, DateTime? from, DateTime? to, int limit);
}
