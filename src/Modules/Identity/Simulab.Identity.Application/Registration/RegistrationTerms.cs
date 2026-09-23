using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Registration;

/// <summary>
/// What every sign-up asks besides the credential (F-4 BR1, BR5; F-20 BR7): an optional name within its column,
/// the 18+ declaration, both acceptances, and the document versions the page showed. One place, so the password
/// sign-up and the Google sign-up can never drift apart.
/// </summary>
public sealed class RegistrationTerms(ILegalDocumentProvider legalDocuments)
{
    /// <summary>The trimmed name, or null when the visitor left it empty.</summary>
    public static string? NormalizeFullName(string? fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();

    /// <summary>
    /// Checks everything without writing anything, in the order the password sign-up always used. A document the
    /// server cannot read at all is a server-side problem, not the visitor's, so its version check is skipped
    /// rather than blocking sign-up.
    /// </summary>
    public async Task<Result> CheckAsync(
        string? fullName,
        bool declaresAdult,
        bool acceptsTerms,
        bool acceptsPrivacy,
        string termsVersion,
        string privacyVersion,
        string locale,
        CancellationToken cancellationToken = default)
    {
        // B-7 BR1, BR3: refused before any account lookup, so the answer is the same for a registered address.
        if (NormalizeFullName(fullName)?.Length > AccountLimits.FullNameMaxLength)
        {
            return Failure(IdentityErrorCodes.RegistrationFullNameTooLong, ErrorKind.Validation);
        }

        if (!declaresAdult)
        {
            return Failure(IdentityErrorCodes.AgeDeclarationRequired, ErrorKind.Validation);
        }

        if (!acceptsTerms || !acceptsPrivacy)
        {
            return Failure(IdentityErrorCodes.ConsentRequired, ErrorKind.Validation);
        }

        var terms = await legalDocuments.GetCurrentAsync(LegalTopic.Terms, locale, cancellationToken);
        var privacy = await legalDocuments.GetCurrentAsync(LegalTopic.Privacy, locale, cancellationToken);

        var outdated =
            (terms is not null && !string.Equals(terms.Version, termsVersion, StringComparison.Ordinal))
            || (privacy is not null && !string.Equals(privacy.Version, privacyVersion, StringComparison.Ordinal));

        return outdated
            ? Failure(IdentityErrorCodes.TermsVersionOutdated, ErrorKind.Conflict)
            : Result.Success();
    }

    private static Result Failure(string code, ErrorKind kind) => Result.Failure(new Error(code, kind));
}
