using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Profile;

/// <summary>
/// The signed-in user's own profile (F-8 UC1-UC3). The caller is always the token subject (BR1): no id
/// comes from the route or the body, so nobody reaches another account's profile.
/// </summary>
public sealed class ProfileHandler(UserManager<User> userManager)
{
    public async Task<ProfileResponse?> GetAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new ProfileResponse(user.Email!, user.FullName, user.PreferredLanguage);
    }

    /// <summary>UC2: name and language together. Nothing is written when either is refused.</summary>
    public async Task<Result> UpdateAsync(Guid userId, string? fullName, string? preferredLanguage)
    {
        var name = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        if (name?.Length > ProfileLimits.FullNameMaxLength)
        {
            return Failure(IdentityErrorCodes.ProfileFullNameTooLong);
        }

        var language = SupportedLanguages.Canonical(preferredLanguage);
        if (language is null)
        {
            return Failure(IdentityErrorCodes.ProfileLanguageNotSupported);
        }

        return await SaveAsync(userId, user =>
        {
            user.FullName = name;
            user.PreferredLanguage = language;
        });
    }

    /// <summary>UC3: the header switch changes the language only; the name stays as it is (BR6).</summary>
    public async Task<Result> UpdatePreferredLanguageAsync(Guid userId, string? preferredLanguage)
    {
        var language = SupportedLanguages.Canonical(preferredLanguage);
        return language is null
            ? Failure(IdentityErrorCodes.ProfileLanguageNotSupported)
            : await SaveAsync(userId, user => user.PreferredLanguage = language);
    }

    private async Task<Result> SaveAsync(Guid userId, Action<User> change)
    {
        // A token that outlived its account (erasure, F-10): the endpoint answers 401, as for any ended session.
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(new Error(IdentityErrorCodes.TokenRevoked, ErrorKind.NotFound));
        }

        change(user);
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            // Both fields were validated above; what is left (a concurrency stamp) is not the caller's to fix.
            throw new InvalidOperationException($"The profile could not be saved: {string.Join(", ", updated.Errors.Select(error => error.Code))}.");
        }

        return Result.Success();
    }

    private static Result Failure(string code) => Result.Failure(new Error(code, ErrorKind.Validation));
}
