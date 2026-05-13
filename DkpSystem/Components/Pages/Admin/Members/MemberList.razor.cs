using Microsoft.AspNetCore.Components;
using DkpSystem.Models;
using DkpSystem.Services;

namespace DkpSystem.Components.Pages.Admin.Members;

public partial class MemberList : ComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private IEnumerable<User>? _members;
    private IEnumerable<Guild>? _guilds;
    private bool _loading = true;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _showRanking = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _loading = true;
        _errorMessage = null;

        try
        {
            _guilds = await MemberService.GetAllGuildsAsync();

            if (_showRanking)
            {
                _members = await MemberService.GetMemberRankingAsync();
            }
            else
            {
                _members = await MemberService.GetAllMembersAsync();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load members: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ShowRanking()
    {
        _showRanking = !_showRanking;
        await LoadDataAsync();
    }

    private void NavigateToDetail(Guid memberId)
    {
        NavigationManager.NavigateTo($"/admin/members/{memberId}");
    }

    private void NavigateToResetPassword(Guid memberId)
    {
        NavigationManager.NavigateTo($"/admin/members/{memberId}/reset-password");
    }

    private async Task DeactivateMember(Guid memberId)
    {
        _errorMessage = null;
        _successMessage = null;

        var result = await MemberService.DeactivateMemberAsync(memberId);

        if (result.IsSuccess)
        {
            _successMessage = "Member deactivated successfully.";
            await LoadDataAsync();
        }
        else
        {
            _errorMessage = result.ErrorMessage;
        }
    }
}
