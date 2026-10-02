using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>
/// The Google step of a sign-in (F-20 UC2, UC3): finds the account for a checked ID token, links an active one, and
/// leaves the second factor and the token issue to the token endpoint, as the password step does. It never creates or
/// activates an account: that is the confirmation's job (BR5, BR7).
/// </summary>
public sealed class GoogleSignInHandler(
    IGoogleIdTokenValidator validator,
    IUserDirectory userDirectory,
    UserManager<User> userManager)
{
    /// <summary>BR2 checks, then BR3: by the Google subject first, then by the address, only where Google vouches for it.</summary>
    public async Task<Result<GoogleSignInOutcome>> SignInAsync(string? idToken, CancellationToken cancellationToken = default)
    {
        var identity = await CheckTokenAsync(validator, idToken, cancellationToken);
        if (identity.IsFailure)
        {
            return Result.Failure<GoogleSignInOutcome>(identity.Error!);
        }

        var google = identity.Value;
        var linked = await userDirectory.FindByLoginIgnoringTenantAsync(GoogleSignInProtocol.LoginProvider, google.Subject, cancellationToken);
        if (linked is not null)
        {
            // UC2, AC6: the link decides, whatever the account's address is today.
            return linked.Status == AccountStatus.Active
                ? Result.Success(GoogleSignInOutcome.Continue(linked))
                : AccountExists();
        }

        var byEmail = await userDirectory.FindByEmailIgnoringTenantAsync(google.Email, cancellationToken);
        if (byEmail is null)
        {
            return Result.Success(GoogleSignInOutcome.SignUp(google));
        }

        // BR3, AC11: an account already linked to another Google subject is never reached through the address.
        // BR4, AC19 (v3): nor is any account, when Google does not vouch for the address.
        var otherKey = await userDirectory.FindLoginKeyAsync(byEmail.Id, GoogleSignInProtocol.LoginProvider, cancellationToken);
        if (otherKey is not null || !google.IsAuthoritative)
        {
            return AccountExists();
        }

        // BR5 (v3): a pending account is taken over on the confirmation page, where its real owner accepts the terms.
        if (byEmail.Status == AccountStatus.Pending)
        {
            return Result.Success(GoogleSignInOutcome.SignUp(google));
        }

        // F-29 BR11 (supersedes F-20 BR4): an active account found by its address is **not** linked here any
        // more. With an explicit link on the Security page, linking on the way in would silently undo a
        // disconnection at the next sign-in, and "disconnect" would not mean what it says. The reader is told
        // the account exists; the text points at the account settings.
        return AccountExists();
    }

    /// <summary>
    /// BR4 with change note v2: only when Google is the last step. With two-factor on, the count and the lockout stay
    /// for the code step, whose lockout rule is shared with the password (F-11 BR10).
    /// </summary>
    // F-47 BR7, no transaction: while a lockout is active the count is already 0, and with a count above 0 any lockout end is past, so a crash between the two writes leaves nothing harmful.
    public async Task ClearFailuresAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
    }

    /// <summary>BR2: the one check both the grant and the confirmation run first.</summary>
    public static async Task<Result<GoogleIdentity>> CheckTokenAsync(
        IGoogleIdTokenValidator validator, string? idToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(validator);

        var identity = await validator.ValidateAsync(idToken, cancellationToken);
        if (identity is null)
        {
            return Result.Failure<GoogleIdentity>(new Error(IdentityErrorCodes.GoogleTokenInvalid, ErrorKind.Validation));
        }

        return identity.EmailVerified
            ? Result.Success(identity)
            : Result.Failure<GoogleIdentity>(new Error(IdentityErrorCodes.GoogleEmailNotVerified, ErrorKind.BusinessRule));
    }

    /// <summary>The <c>user_logins</c> row of a Google link: the provider and Google's subject.</summary>
    public static UserLoginInfo Login(GoogleIdentity google)
    {
        ArgumentNullException.ThrowIfNull(google);
        return new UserLoginInfo(GoogleSignInProtocol.LoginProvider, google.Subject, GoogleSignInProtocol.LoginProvider);
    }

    private static Result<GoogleSignInOutcome> AccountExists() =>
        Result.Failure<GoogleSignInOutcome>(new Error(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict));
}
