using DkpSystem.Models.ViewModels;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;

namespace DkpSystem.Components.Pages.Auth;

public partial class Register : ComponentBase
{
    [Inject] private IAuthenticationService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromForm]
    private RegisterModel _model { get; set; } = new();

    private string _errorMessage = string.Empty;
    private bool _isProcessing = false;

    private async Task HandleRegister()
    {
        _isProcessing = true;
        _errorMessage = string.Empty;


            // Validate passwords match
            if (_model.Password != _model.ConfirmPassword)
            {
                _errorMessage = "Passwords do not match.";
                _isProcessing = false;
                return;
            }

            var (success, errors) = await AuthService.RegisterAsync(_model.Email, _model.Username, _model.Password, _model.InvitationCode);

            if (success)
            {
                // Redirect to home page - use NavigateTo with forceLoad
                // to ensure the authentication cookie is properly loaded
                await InvokeAsync(() => Navigation.NavigateTo("/"));
            }
            else
            {
                _errorMessage = string.Join(" ", errors);
                _isProcessing = false;
            }

    }
}
