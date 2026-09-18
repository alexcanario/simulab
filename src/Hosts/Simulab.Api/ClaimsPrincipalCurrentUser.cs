using Simulab.SharedKernel.Security;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Simulab.Api;

/// <summary>The signed-in caller (F-5), read from the access token's <c>sub</c> claim. Anonymous while there is none.</summary>
public sealed class ClaimsPrincipalCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst(Claims.Subject)?.Value;
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
