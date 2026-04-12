using System.Security.Claims;
using DkpSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DkpSystem.Data.Identity;

/// <summary>
/// Custom claims principal factory that adds the user's role as a claim.
/// </summary>
public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUserClaimsPrincipalFactory"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="optionsAccessor">The identity options.</param>
    public CustomUserClaimsPrincipalFactory(
        UserManager<User> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    /// <summary>
    /// Generates claims for the user, including their role.
    /// </summary>
    /// <param name="user">The user to generate claims for.</param>
    /// <returns>A ClaimsPrincipal containing the user's claims.</returns>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        
        // Add role claim
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
        
        // Add username claim for display purposes
        identity.AddClaim(new Claim("username", user.Username));
        
        return identity;
    }
}
