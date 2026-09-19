using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Passwords;

/// <summary>F-7 BR7, shared by the reset and the change: the policy of F-4 BR2, and not the current password.</summary>
internal static class PasswordRules
{
    /// <summary>The refusal for this new password, or null when it may be set. Checking it never counts as a failed sign-in.</summary>
    public static async Task<Error?> CheckNewPasswordAsync(
        UserManager<User> userManager,
        User user,
        string? newPassword,
        string tooWeakCode,
        string sameAsCurrentCode)
    {
        if (string.IsNullOrEmpty(newPassword))
        {
            return new Error(tooWeakCode, ErrorKind.Validation);
        }

        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, newPassword);
            if (!result.Succeeded)
            {
                return new Error(tooWeakCode, ErrorKind.Validation);
            }
        }

        return await userManager.HasPasswordAsync(user) && await userManager.CheckPasswordAsync(user, newPassword)
            ? new Error(sameAsCurrentCode, ErrorKind.BusinessRule)
            : null;
    }
}
