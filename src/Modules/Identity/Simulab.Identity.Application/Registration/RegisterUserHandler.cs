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
/// was already registered (BR4): the caller cannot tell a new account from an existing one. Every write
/// - the user, the Student role, the consent record, the verification token and the staged email job -
/// commits in one transaction, or none of it does (F-30 BR1).
/// </summary>
public sealed class RegisterUserHandler(
    UserManager<User> userManager,
    IUserDirectory userDirectory,
    IEmailVerificationTokenStore tokenStore,
    IConsentRecordStore consentStore,
    RegistrationTerms terms,
    IVerificationMailer mailer,
    IIdentityUnitOfWork unitOfWork,
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

        var (rawToken, tokenHash) = SecureToken.Generate();

        // F-30 BR9: the transaction opens only now, after every validation, lookup and outbound call.
        await using var transaction = await unitOfWork.BeginAsync(cancellationToken);
        try
        {
            var created = await userManager.CreateAsync(user, command.Password);
            if (!created.Succeeded)
            {
                return FromIdentityErrors(created);
            }

            // F-30 BR4: every new account starts as a Student; there is no other way to sign up. A missing
            // role makes AddToRoleAsync throw instead of failing, and either way the transaction is never
            // committed below, so no half account is left behind.
            var addedToRole = await userManager.AddToRoleAsync(user, IdentityRoles.Student);
            if (!addedToRole.Succeeded)
            {
                throw new InvalidOperationException("The Student role could not be attached to a new account.");
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

            // F-13 BR2: the mailer only stages the job. It runs before the store, whose save writes the
            // token and the job under the same transaction: a valid link never exists without the email
            // that carries it, and now neither exists without the account (F-30 BR7).
            await mailer.SendAsync(user.Email!, rawToken, user.PreferredLanguage, cancellationToken);

            await tokenStore.AddAsync(
                new EmailVerificationToken
                {
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    ExpiresAt = now.Add(VerificationTokenPolicy.Lifetime)
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (unitOfWork.TranslateWriteFailure(exception) == IdentityUniqueViolation.UserEmail)
        {
            // F-30 BR6: the address was taken by another writer between the lookup above and this
            // transaction's insert. The transaction is left uncommitted (rolled back on disposal) and the
            // caller gets the same answer as BR4: it cannot tell the two races apart either.
            logger.LogInformation("Sign-up raced another writer for the same address; nothing was created.");
            return Result.Success();
        }
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
