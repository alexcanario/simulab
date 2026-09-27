using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>
/// F-29: the signed-in account's own Google link — read it, add it, remove it. Every method acts on the
/// caller's account and takes no user id from a request (BR1 to BR10).
/// </summary>
public sealed class GoogleLinkHandler(
    IGoogleIdTokenValidator validator,
    IUserDirectory userDirectory,
    UserManager<User> userManager,
    IAccountEventLog accountEvents,
    TimeProvider timeProvider)
{
    /// <summary>The state the Security page shows (BR12).</summary>
    public async Task<Result<GoogleLinkResponse>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Success(new GoogleLinkResponse(false));
        }

        var login = await FindGoogleLoginAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);
        return Result.Success(login is null
            ? new GoogleLinkResponse(false, null, hasPassword)
            : new GoogleLinkResponse(true, AddressOf(login), hasPassword));
    }

    /// <summary>
    /// BR2 to BR7. The ID token is checked exactly as a sign-in checks it; what differs is that the account is
    /// already known, so the address is never used to find one (BR4).
    /// </summary>
    public async Task<Result> LinkAsync(Guid userId, string? idToken, CancellationToken cancellationToken = default)
    {
        var identity = await GoogleSignInHandler.CheckTokenAsync(validator, idToken, cancellationToken);
        if (identity.IsFailure)
        {
            return Result.Failure(identity.Error!);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Failure(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict);
        }

        var google = identity.Value;
        var mine = await FindGoogleLoginAsync(user);
        if (mine is not null)
        {
            // BR6: the same subject again is the double round trip of two tabs, and changes nothing.
            return mine.ProviderKey == google.Subject
                ? Result.Success()
                : Failure(IdentityErrorCodes.GoogleLinkAlreadyLinked, ErrorKind.Conflict);
        }

        // BR5: the subject belongs to somebody else; neither account changes.
        var owner = await userDirectory.FindByLoginIgnoringTenantAsync(
            GoogleSignInProtocol.LoginProvider, google.Subject, cancellationToken);
        if (owner is not null)
        {
            return Failure(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict);
        }

        // BR12: the address goes in the display name, which until now held the provider's own name.
        var added = await userManager.AddLoginAsync(
            user, new UserLoginInfo(GoogleSignInProtocol.LoginProvider, google.Subject, google.Email));
        if (!added.Succeeded)
        {
            // The same subject was claimed a moment ago by another account (two tabs, two accounts): the
            // primary key refused it. The same answer as finding the owner above, never a 500.
            return Failure(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict);
        }

        await accountEvents.RecordAsync(user.Id, AccountEventType.GoogleLinked, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// BR8 to BR10, in the order BR10 fixes: no link first, so an account with neither link nor password is
    /// told nothing it did not already know; then the password, with the same lockout every other
    /// password-confirmed action of this module uses.
    /// </summary>
    public async Task<Result> UnlinkAsync(Guid userId, string? currentPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Success();
        }

        var login = await FindGoogleLoginAsync(user);
        if (login is null)
        {
            // BR9, BR10: nothing to remove, and nothing else is read.
            return Result.Success();
        }

        // BR9: the link is the only way in, so removing it would lock the account out of itself.
        if (!await userManager.HasPasswordAsync(user))
        {
            return Failure(IdentityErrorCodes.PasswordNotSet, ErrorKind.BusinessRule);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return await LockedAsync(user);
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            await userManager.AccessFailedAsync(user);
            if (!await userManager.IsLockedOutAsync(user))
            {
                return Failure(IdentityErrorCodes.GoogleLinkCurrentPasswordInvalid, ErrorKind.BusinessRule);
            }

            // F-21 BR5: the refused attempt is not an event of its own, but the lockout it caused is.
            await accountEvents.AccountLockedAsync(user.Id, cancellationToken);
            return await LockedAsync(user);
        }

        var removed = await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        if (!removed.Succeeded)
        {
            // Another tab removed it first: the end state is the one the caller asked for.
            return Result.Success();
        }

        await accountEvents.RecordAsync(user.Id, AccountEventType.GoogleUnlinked, cancellationToken);
        return Result.Success();
    }

    private async Task<UserLoginInfo?> FindGoogleLoginAsync(User user)
    {
        var logins = await userManager.GetLoginsAsync(user);
        return logins.FirstOrDefault(login =>
            login.LoginProvider == GoogleSignInProtocol.LoginProvider);
    }

    /// <summary>
    /// BR12: a row written before F-29 holds the provider's own name, which is not an address. It is compared
    /// exactly, not searched for an "@": the stored value is data, not a pattern to guess at.
    /// </summary>
    private static string? AddressOf(UserLoginInfo login) =>
        string.IsNullOrWhiteSpace(login.ProviderDisplayName)
        || login.ProviderDisplayName == GoogleSignInProtocol.LoginProvider
            ? null
            : login.ProviderDisplayName;

    /// <summary><see cref="IdentityErrorCodes.AccountLocked"/> with the remaining whole seconds, as an erasure does.</summary>
    private async Task<Result> LockedAsync(User user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user) ?? timeProvider.GetUtcNow();
        var remaining = Math.Max(0, (int)Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow()).TotalSeconds));
        return Result.Failure(new Error(IdentityErrorCodes.AccountLocked, ErrorKind.BusinessRule, remaining.ToString(CultureInfo.InvariantCulture)));
    }

    private static Result Failure(string code, ErrorKind kind) => Result.Failure(new Error(code, kind));
}
