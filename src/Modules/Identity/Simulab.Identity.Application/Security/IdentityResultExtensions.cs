using Microsoft.AspNetCore.Identity;

namespace Simulab.Identity.Application.Security;

internal static class IdentityResultExtensions
{
    /// <summary>
    /// F-47 BR1b: after the checks passed, a failed <see cref="IdentityResult"/> is not an answer to give but a
    /// reason to roll back, so it is thrown; the host answers its problem-details 500 (BR5).
    /// </summary>
    public static void ThrowIfFailed(this IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"{action} failed after the checks passed: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }
    }
}
