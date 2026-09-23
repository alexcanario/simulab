using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>
/// The confirmation of a first Google sign-in (F-20 UC1, UC4, BR5, BR7): creates the account, or takes over a pending
/// one at an address Google vouches for, with the same terms, privacy and 18+ rules as the password sign-up. Either
/// way the account ends active, without a password, linked to the Google subject, and no verification email is sent.
/// </summary>
public sealed class RegisterGoogleUserHandler(
    IGoogleIdTokenValidator validator,
    RegistrationTerms terms,
    IUserDirectory userDirectory,
    UserManager<User> userManager,
    IConsentRecordStore consentStore,
    TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(RegisterGoogleUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Every input check runs before the lookup that tells a new address from a known one (rule: api-contracts).
        var termsCheck = await terms.CheckAsync(
            command.FullName, command.DeclaresAdult, command.AcceptsTerms, command.AcceptsPrivacy,
            command.TermsVersion, command.PrivacyVersion, command.Locale, cancellationToken);
        if (termsCheck.IsFailure)
        {
            return termsCheck;
        }

        var identity = await GoogleSignInHandler.CheckTokenAsync(validator, command.IdToken, cancellationToken);
        if (identity.IsFailure)
        {
            return identity;
        }

        var google = identity.Value;
        var now = timeProvider.GetUtcNow();

        // BR9: an account linked to this subject appeared after the Google step; the visitor signs in again instead.
        if (await userDirectory.FindByLoginIgnoringTenantAsync(GoogleSignInProtocol.LoginProvider, google.Subject, cancellationToken) is not null)
        {
            return AccountExists();
        }

        var existing = await userDirectory.FindByEmailIgnoringTenantAsync(google.Email, cancellationToken);
        var user = existing is null
            ? await CreateAsync(google, command, now)
            : await TakeOverAsync(existing, google, command, now);
        if (user is null)
        {
            return AccountExists();
        }

        await consentStore.AddAsync(
            new ConsentRecord
            {
                UserId = user.Id,
                TermsVersion = command.TermsVersion,
                PrivacyVersion = command.PrivacyVersion,
                DeclaresAdult = command.DeclaresAdult,
                Locale = command.Locale,
                AcceptedAt = now,
                IpAddress = command.IpAddress
            },
            cancellationToken);

        return Result.Success();
    }

    /// <summary>BR7: a new account, active from the start because Google proved the address.</summary>
    private async Task<User?> CreateAsync(GoogleIdentity google, RegisterGoogleUserCommand command, DateTimeOffset now)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = google.Email,
            Email = google.Email,
            FullName = RegistrationTerms.NormalizeFullName(command.FullName),
            IsAdultDeclared = command.DeclaresAdult,
            PreferredLanguage = command.Locale
        };
        user.VerifyEmail(now);

        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            // The unique index caught an account created between the lookup and the insert (BR9).
            return null;
        }

        // F-4 BR2: every new account starts as a Student; there is no other way to sign up.
        await userManager.AddToRoleAsync(user, IdentityRoles.Student);

        var linked = await userManager.AddLoginAsync(user, GoogleSignInHandler.Login(google));
        return linked.Succeeded
            ? user
            : throw new InvalidOperationException("The Google login of a new account could not be linked.");
    }

    /// <summary>
    /// BR5 (change note v3): the pending account at an address Google vouches for becomes this visitor's. The link comes
    /// first, so a race lost here changes nothing; then the password, name and 18+ declaration nobody proved give way to
    /// what this visitor sent. An active account, or an address Google does not vouch for, is not taken (BR4, BR9).
    /// </summary>
    private async Task<User?> TakeOverAsync(User user, GoogleIdentity google, RegisterGoogleUserCommand command, DateTimeOffset now)
    {
        if (user.Status != AccountStatus.Pending || !google.IsAuthoritative)
        {
            return null;
        }

        var linked = await userManager.AddLoginAsync(user, GoogleSignInHandler.Login(google));
        if (!linked.Succeeded)
        {
            return null;
        }

        user.VerifyEmail(now);
        user.FullName = RegistrationTerms.NormalizeFullName(command.FullName);
        user.IsAdultDeclared = command.DeclaresAdult;

        // RemovePasswordAsync renews the security stamp and saves the changes above with it.
        var removed = await userManager.RemovePasswordAsync(user);
        return removed.Succeeded
            ? user
            : throw new InvalidOperationException("The password of a pending account could not be removed.");
    }

    private static Result AccountExists() =>
        Result.Failure(new Error(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict));
}
