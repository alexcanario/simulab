using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Registration;

/// <summary>
/// Creates an account and sends its verification link (UC1). The answer never says whether the address
/// was already registered (BR4): the caller cannot tell a new account from an existing one.
/// </summary>
public sealed class RegisterUserHandler(
    UserManager<User> userManager,
    IUserDirectory userDirectory,
    IEmailVerificationTokenStore tokenStore,
    IConsentRecordStore consentStore,
    ILegalDocumentProvider legalDocuments,
    IVerificationMailer mailer,
    TimeProvider timeProvider,
    ILogger<RegisterUserHandler> logger)
{
    private static readonly EmailAddressAttribute EmailFormat = new();

    public async Task<Result> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email) || !EmailFormat.IsValid(command.Email))
        {
            return Failure(IdentityErrorCodes.EmailInvalid, ErrorKind.Validation);
        }

        // BR1: the three acceptances are checked before anything is written. The endpoint validates them
        // too; this is the barrier for any other caller of the handler.
        if (!command.DeclaresAdult)
        {
            return Failure(IdentityErrorCodes.AgeDeclarationRequired, ErrorKind.Validation);
        }

        if (!command.AcceptsTerms || !command.AcceptsPrivacy)
        {
            return Failure(IdentityErrorCodes.ConsentRequired, ErrorKind.Validation);
        }

        var versionCheck = await CheckDocumentVersionsAsync(command, cancellationToken);
        if (versionCheck.IsFailure)
        {
            return versionCheck;
        }

        var existing = await userDirectory.FindByEmailIgnoringTenantAsync(command.Email, cancellationToken);
        if (existing is not null)
        {
            // BR4: the same success the caller would get for a new account. Only the log knows the difference.
            logger.LogInformation("Sign-up for an address that is already registered; nothing was created.");
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = command.Email,
            Email = command.Email,
            FullName = string.IsNullOrWhiteSpace(command.FullName) ? null : command.FullName.Trim(),
            IsAdultDeclared = command.DeclaresAdult,
            PreferredLanguage = command.Locale
        };

        var created = await userManager.CreateAsync(user, command.Password);
        if (!created.Succeeded)
        {
            return FromIdentityErrors(created);
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

        var (rawToken, tokenHash) = SecureToken.Generate();
        await tokenStore.AddAsync(
            new EmailVerificationToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = now.Add(VerificationTokenPolicy.Lifetime)
            },
            cancellationToken);

        await mailer.SendAsync(user.Email!, rawToken, user.PreferredLanguage, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// BR5: the form carries the versions the page showed. A document the server cannot read at all is a
    /// server-side problem, not the visitor's, so the check is skipped rather than blocking sign-up.
    /// </summary>
    private async Task<Result> CheckDocumentVersionsAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var terms = await legalDocuments.GetCurrentAsync(LegalTopic.Terms, command.Locale, cancellationToken);
        var privacy = await legalDocuments.GetCurrentAsync(LegalTopic.Privacy, command.Locale, cancellationToken);

        var outdated =
            (terms is not null && !string.Equals(terms.Version, command.TermsVersion, StringComparison.Ordinal))
            || (privacy is not null && !string.Equals(privacy.Version, command.PrivacyVersion, StringComparison.Ordinal));

        return outdated
            ? Failure(IdentityErrorCodes.TermsVersionOutdated, ErrorKind.Conflict)
            : Result.Success();
    }

    /// <summary>
    /// Identity reports a duplicate as an error. Reaching it means the address was taken between the
    /// lookup and the insert, and the answer must stay the one BR4 describes.
    /// </summary>
    private static Result FromIdentityErrors(IdentityResult result)
    {
        if (result.Errors.Any(error => error.Code.StartsWith("Duplicate", StringComparison.Ordinal)))
        {
            return Result.Success();
        }

        return result.Errors.Any(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
            ? Failure(IdentityErrorCodes.PasswordTooWeak, ErrorKind.Validation)
            : Failure(IdentityErrorCodes.EmailInvalid, ErrorKind.Validation);
    }

    private static Result Failure(string code, ErrorKind kind) => Result.Failure(new Error(code, kind));
}
