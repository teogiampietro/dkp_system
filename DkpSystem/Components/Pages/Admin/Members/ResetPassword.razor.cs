using Microsoft.AspNetCore.Components;
using DkpSystem.Models;
using DkpSystem.Models.ViewModels;
using DkpSystem.Services;

namespace DkpSystem.Components.Pages.Admin.Members;

public partial class ResetPassword : ComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public Guid Id { get; set; }

    private User? _member;
    private bool _loading = true;
    private bool _resetting = false;
    private string? _errorMessage;
    private string? _successMessage;
    private ResetPasswordModel _resetModel = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadMemberAsync();
    }

    private async Task LoadMemberAsync()
    {
        _loading = true;
        _errorMessage = null;

        try
        {
            _member = await MemberService.GetMemberByIdAsync(Id);
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

    private async Task HandleResetPassword()
    {
        if (_member == null) return;

        _resetting = true;
        _errorMessage = null;
        _successMessage = null;

        try
        {
            // Validate passwords match
            if (_resetModel.TemporaryPassword != _resetModel.ConfirmPassword)
            {
                _errorMessage = "Passwords do not match.";
                _resetting = false;
                return;
            }

            var result = await MemberService.ResetMemberPasswordAsync(Id, _resetModel.TemporaryPassword);

            if (result.IsSuccess)
            {
                _successMessage = $"Password reset successfully for {_member.Username}. Please communicate the temporary password to them securely.";
                _resetModel = new ResetPasswordModel(); // Clear form
            }
            else
            {
                _errorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to reset password: {ex.Message}";
        }
        finally
        {
            _resetting = false;
        }
    }

    private void NavigateBack()
    {
        NavigationManager.NavigateTo($"/admin/members/{Id}");
    }
}
