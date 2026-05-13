using System.Security.Claims;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DkpSystem.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private int? _dkpBalance;

    protected override async Task OnParametersSetAsync()
    {
        if (AuthenticationStateTask is null) return;

        try
        {
            var authState = await AuthenticationStateTask;
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await MemberService.GetMemberByIdAsync(userId);
                _dkpBalance = user?.DkpBalance;
            }
            else
            {
                _dkpBalance = null;
            }
        }
        catch
        {
            _dkpBalance = null;
        }
    }
}
