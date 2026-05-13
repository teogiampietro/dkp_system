using Microsoft.AspNetCore.Components;
using DkpSystem.Data.Repositories;
using DkpSystem.Models;
using DkpSystem.Models.ViewModels;
using DkpSystem.Services;

namespace DkpSystem.Components.Pages.Admin.Members;

public partial class MemberDetail : ComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public Guid Id { get; set; }

    private User? _member;
    private IEnumerable<Guild>? _guilds;
    private IEnumerable<MemberEarningHistory>? _earningsHistory;
    private IEnumerable<MemberWonItemHistory>? _wonItemsHistory;
    private bool _loading = true;
    private bool _saving = false;
    private string? _errorMessage;
    private string? _successMessage;
    private MemberEditModel _editModel = new();

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
            _member = await MemberService.GetMemberByIdAsync(Id);

            if (_member != null)
            {
                _guilds = await MemberService.GetAllGuildsAsync();
                _earningsHistory = await MemberService.GetMemberEarningsHistoryAsync(Id);
                _wonItemsHistory = await MemberService.GetMemberWonItemsHistoryAsync(Id);

                // Initialize edit model
                _editModel.Role = _member.Role;
                _editModel.GuildId = _member.GuildId?.ToString() ?? "";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load member: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SaveChanges()
    {
        if (_member == null) return;

        _saving = true;
        _errorMessage = null;
        _successMessage = null;

        try
        {
            Guid? guildId = string.IsNullOrEmpty(_editModel.GuildId) ? null : Guid.Parse(_editModel.GuildId);

            var result = await MemberService.UpdateMemberRoleAndGuildAsync(Id, _editModel.Role, guildId);

            if (result.IsSuccess)
            {
                _successMessage = "Member updated successfully.";
                await LoadDataAsync();
            }
            else
            {
                _errorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to update member: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }

    private void NavigateBack()
    {
        NavigationManager.NavigateTo("/admin/members");
    }
}
