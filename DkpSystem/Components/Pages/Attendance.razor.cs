using System.Security.Claims;
using DkpSystem.Models;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Pages;

public partial class Attendance : ComponentBase
{
    [Inject] private IAttendanceService AttendanceService { get; set; } = default!;
    [Inject] private IMemberService MemberService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private bool _isLoading = true;
    private string? _errorMessage;
    private Guid? _guildId;
    private AttendanceMatrix? _matrix;

    private DateTime? _from;
    private DateTime? _to;
    private int _limit = 20;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthenticationStateTask!;
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _errorMessage = "Could not identify the current user.";
                return;
            }

            var currentUser = await MemberService.GetMemberByIdAsync(userId);
            if (currentUser?.GuildId == null)
            {
                _errorMessage = "You are not assigned to a guild. Please contact an admin.";
                return;
            }

            _guildId = currentUser.GuildId;
            await LoadMatrixAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load attendance: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadMatrixAsync()
    {
        if (_guildId == null)
        {
            return;
        }

        _matrix = await AttendanceService.GetAttendanceMatrixAsync(_guildId.Value, _from, _to, _limit);
    }

    private async Task ApplyFiltersAsync()
    {
        if (_from.HasValue && _to.HasValue && _from > _to)
        {
            _errorMessage = "'From' date cannot be after 'To' date.";
            return;
        }

        _errorMessage = null;
        _isLoading = true;
        try
        {
            await LoadMatrixAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load attendance: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task ClearFiltersAsync()
    {
        _from = null;
        _to = null;
        _limit = 20;
        await ApplyFiltersAsync();
    }
}
