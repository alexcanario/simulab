using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>
/// Creates the account of a first Google sign-in (F-20 UC1, BR7): active, without a password, linked to the Google
/// subject, with the same terms, privacy and 18+ rules as the password sign-up and no verification email.
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

        // BR9: an account for this subject or address appeared after the Google step; the visitor signs in again instead.
        if (await userDirectory.FindByLoginIgnoringTenantAsync(GoogleSignInProtocol.LoginProvider, google.Subject, cancellationToken) is not null
            || await userDirectory.FindByEmailIgnoringTenantAsync(google.Email, cancellationToken) is not null)
        {
            return AccountExists();
        }

        var now = timeProvider.GetUtcNow();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = google.Email,
            Email = google.Email,
            FullName = RegistrationTerms.NormalizeFullName(command.FullName),
            IsAdultDeclared = command.DeclaresAdult,
            PreferredLanguage = command.Locale
        };

        // BR7: Google proved the address, so the account starts active and no verification email is sent.
        user.VerifyEmail(now);

        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            // The unique index caught an account created between the lookup and the insert (BR9).
            return AccountExists();
        }

        // F-4 BR2: every new account starts as a Student; there is no other way to sign up.
        await userManager.AddToRoleAsync(user, IdentityRoles.Student);

        var linked = await userManager.AddLoginAsync(
            user, new UserLoginInfo(GoogleSignInProtocol.LoginProvider, google.Subject, GoogleSignInProtocol.LoginProvider));
        if (!linked.Succeeded)
        {
            throw new InvalidOperationException("The Google login of a new account could not be linked.");
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

    private static Result AccountExists() =>
        Result.Failure(new Error(IdentityErrorCodes.GoogleAccountExists, ErrorKind.Conflict));
}
