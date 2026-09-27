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
/// Every write commits in one transaction, or none of it does (F-30 BR2).
/// </summary>
public sealed class RegisterGoogleUserHandler(
    IGoogleIdTokenValidator validator,
    RegistrationTerms terms,
    IUserDirectory userDirectory,
    UserManager<User> userManager,
    IConsentRecordStore consentStore,
    IIdentityUnitOfWork unitOfWork,
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

        // F-30 BR9: the Google step, the one outbound call this handler makes, finishes before the
        // transaction opens below.
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

        await using var transaction = await unitOfWork.BeginAsync(cancellationToken);
        try
        {
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

            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (unitOfWork.TranslateWriteFailure(exception) is { } violation)
        {
            // F-30 BR6: the transaction is left uncommitted (rolled back on disposal) either way.
            return violation switch
            {
                // The address was taken by another writer between the lookup above and this transaction's
                // insert: the same answer BR4 gives the password sign-up for a known address.
                IdentityUniqueViolation.UserEmail => Result.Success(),
                // The Google subject was linked to another account in that same window.
                _ => AccountExists(),
            };
        }
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

        // F-4 BR2, F-30 BR4: every new account starts as a Student; there is no other way to sign up. A
        // missing role makes AddToRoleAsync throw instead of failing, and either way the caller's
        // transaction is never committed, so no half account is left behind.
        var addedToRole = await userManager.AddToRoleAsync(user, IdentityRoles.Student);
        if (!addedToRole.Succeeded)
        {
            throw new InvalidOperationException("The Student role could not be attached to a new account.");
        }

        var linked = await userManager.AddLoginAsync(user, GoogleSignInHandler.Login(google));
        return linked.Succeeded
            ? user
            : throw new InvalidOperationException("The Google login of a new account could not be linked.");
    }

    /// <summary>
    /// BR5 (change note v3): the pending account at an address Google vouches for becomes this visitor's. Since F-30,
    /// one transaction holds every write here, so nothing this request writes is visible to a concurrent one until it
    /// commits (BR6b): the link, the password removal and the profile changes below either all land together or none
    /// does. An active account, or an address Google does not vouch for, is not taken (BR4, BR9).
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
