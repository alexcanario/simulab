using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>
/// The Google step of a sign-in (F-20 UC2-UC4): finds the account for a checked ID token, links it, and leaves the
/// second factor and the token issue to the token endpoint, as the password step does.
/// </summary>
public sealed class GoogleSignInHandler(
    IGoogleIdTokenValidator validator,
    IUserDirectory userDirectory,
    UserManager<User> userManager,
    TimeProvider timeProvider)
{
    /// <summary>BR2 checks, then BR3: by the Google subject first, then by the address. Never creates an account (BR7).</summary>
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
            return await ContinueAsync(linked, google, alreadyLinked: true);
        }

        var byEmail = await userDirectory.FindByEmailIgnoringTenantAsync(google.Email, cancellationToken);
        if (byEmail is null)
        {
            return Result.Success(GoogleSignInOutcome.SignUp(google));
        }

        // BR3, AC11: an account already linked to another Google subject is never reached through the address.
        var otherKey = await userDirectory.FindLoginKeyAsync(byEmail.Id, GoogleSignInProtocol.LoginProvider, cancellationToken);
        if (otherKey is not null)
        {
            return Result.Failure<GoogleSignInOutcome>(new Error(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict));
        }

        return await ContinueAsync(byEmail, google, alreadyLinked: false);
    }

    /// <summary>
    /// BR4 with change note v2: only when Google is the last step. With two-factor on, the count and the lockout stay
    /// for the code step, whose lockout rule is shared with the password (F-11 BR10).
    /// </summary>
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

    private async Task<Result<GoogleSignInOutcome>> ContinueAsync(User user, GoogleIdentity google, bool alreadyLinked)
    {
        if (user.Status == AccountStatus.Pending)
        {
            // BR5, UC4: Google proved the address; whoever set the password never did. RemovePasswordAsync renews
            // the security stamp and saves the status change with it.
            user.VerifyEmail(timeProvider.GetUtcNow());
            var removed = await userManager.RemovePasswordAsync(user);
            if (!removed.Succeeded)
            {
                throw new InvalidOperationException("The password of a pending account could not be removed.");
            }
        }
        else if (user.Status != AccountStatus.Active)
        {
            return Result.Failure<GoogleSignInOutcome>(new Error(IdentityErrorCodes.GoogleTokenInvalid, ErrorKind.Validation));
        }

        if (!alreadyLinked)
        {
            var added = await userManager.AddLoginAsync(user, new UserLoginInfo(GoogleSignInProtocol.LoginProvider, google.Subject, GoogleSignInProtocol.LoginProvider));
            if (!added.Succeeded)
            {
                // The same subject was linked to another account a moment ago (two tabs): nothing to sign in to here.
                return Result.Failure<GoogleSignInOutcome>(new Error(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict));
            }
        }

        return Result.Success(GoogleSignInOutcome.Continue(user));
    }
}
