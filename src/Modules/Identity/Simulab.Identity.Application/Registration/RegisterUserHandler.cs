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
    RegistrationTerms terms,
    IVerificationMailer mailer,
    TimeProvider timeProvider,
    ILogger<RegisterUserHandler> logger)
{
    private static readonly EmailAddressAttribute EmailFormat = new();

    public async Task<Result> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // B-7 BR2: an address longer than its column is not a valid address.
        if (string.IsNullOrWhiteSpace(command.Email)
            || command.Email.Length > AccountLimits.EmailMaxLength
            || !EmailFormat.IsValid(command.Email))
        {
            return Failure(IdentityErrorCodes.EmailInvalid, ErrorKind.Validation);
        }

        // B-7 BR1, BR3 and BR1, BR5: the name, the three acceptances and the versions are checked before the
        // account lookup below and before anything is written. The endpoint validates them too; this is the
        // barrier for any other caller of the handler.
        var termsCheck = await terms.CheckAsync(
            command.FullName, command.DeclaresAdult, command.AcceptsTerms, command.AcceptsPrivacy,
            command.TermsVersion, command.PrivacyVersion, command.Locale, cancellationToken);
        if (termsCheck.IsFailure)
        {
            return termsCheck;
        }

        var fullName = RegistrationTerms.NormalizeFullName(command.FullName);

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
            FullName = fullName,
            IsAdultDeclared = command.DeclaresAdult,
            PreferredLanguage = command.Locale
        };

        var created = await userManager.CreateAsync(user, command.Password);
        if (!created.Succeeded)
        {
            return FromIdentityErrors(created);
        }

        // BR2: every new account starts as a Student; there is no other way to sign up.
        await userManager.AddToRoleAsync(user, IdentityRoles.Student);

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

        // F-13 BR2: the mailer only stages the job. It runs before the store, whose save writes the token
        // and the job in one transaction: a valid link never exists without the email that carries it.
        await mailer.SendAsync(user.Email!, rawToken, user.PreferredLanguage, cancellationToken);

        await tokenStore.AddAsync(
            new EmailVerificationToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = now.Add(VerificationTokenPolicy.Lifetime)
            },
            cancellationToken);

        return Result.Success();
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
