using DkpSystem.Models;
using DkpSystem.Models.ViewModels;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Pages;

public partial class Profile : ComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private User? _user;
    private IEnumerable<MemberEarningHistory>? _earningsHistory;
    private IEnumerable<MemberWonItemHistory>? _wonItemsHistory;
    private bool _isLoading = true;
    private ChangePasswordModel _passwordModel = new();
    private string _passwordMessage = string.Empty;
    private bool _passwordSuccess = false;
    private bool _isChangingPassword = false;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserProfile();
    }

    private async Task LoadUserProfile()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    _user = await MemberService.GetMemberByIdAsync(userId);

                    if (_user != null)
                    {
                        _earningsHistory = await MemberService.GetMemberEarningsHistoryAsync(userId);
                        _wonItemsHistory = await MemberService.GetMemberWonItemsHistoryAsync(userId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load profile: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleChangePassword()
    {
        _isChangingPassword = true;
        _passwordMessage = string.Empty;

        try
        {
            if (_passwordModel.NewPassword != _passwordModel.ConfirmNewPassword)
            {
                _passwordMessage = "New passwords do not match.";
                _passwordSuccess = false;
                return;
            }

            if (_user == null)
            {
                _passwordMessage = "User not found.";
                _passwordSuccess = false;
                return;
            }

            var result = await MemberService.ChangeOwnPasswordAsync(
                _user.Id,
                _passwordModel.CurrentPassword,
                _passwordModel.NewPassword);

            if (result.IsSuccess)
            {
                _passwordMessage = "Password changed successfully!";
                _passwordSuccess = true;
                _passwordModel = new(); // Reset form
            }
            else
            {
                _passwordMessage = result.ErrorMessage ?? "Failed to change password.";
                _passwordSuccess = false;
            }
        }
        catch (Exception ex)
        {
            _passwordMessage = $"An error occurred: {ex.Message}";
            _passwordSuccess = false;
        }
        finally
        {
            _isChangingPassword = false;
        }
    }
}
