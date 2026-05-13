using DkpSystem.Models.ViewModels;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Pages.Auth;

public partial class Login : ComponentBase
{
    [Inject] private IAuthenticationService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [SupplyParameterFromForm] private LoginModel _model { get; set; } = new();

    private string _errorMessage = string.Empty;
    private bool _isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            Navigation.NavigateTo("/dashboard");
        }
    }

    private async Task HandleLogin()
    {
        _isProcessing = true;
        await Task.Delay(1000);
        _errorMessage = string.Empty;

        var (success, error) = await AuthService.LoginAsync(_model.Email, _model.Password);

        if (success)
        {
            await InvokeAsync(() => Navigation.NavigateTo("/"));
        }
        else
        {
            _errorMessage = error;
            _isProcessing = false;
        }
    }
}
