using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.LegalAcceptance;

/// <summary>
/// A signed-in user accepts the current terms and privacy policy again (F-85). The status says what is owed; the
/// acceptance writes a new <see cref="ConsentRecord"/> and never changes an older one (F-4 BR6).
/// </summary>
public sealed class LegalAcceptanceHandler(
    LegalDocumentReader documents,
    IConsentRecordStore consents,
    UserManager<User> userManager,
    TimeProvider timeProvider)
{
    /// <summary>The user's status, or null when the account no longer exists.</summary>
    public async Task<LegalAcceptanceStatusResponse?> GetStatusAsync(Guid userId, string locale, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByIdAsync(userId.ToString()) is null)
        {
            return null;
        }

        var current = await documents.GetCurrentAsync(locale, cancellationToken);
        var latest = await consents.FindLatestAsync(userId, cancellationToken);

        var list = new List<LegalAcceptanceDocumentResponse>(2);
        if (current.Terms is { } terms)
        {
            list.Add(Describe(terms, latest is null || !Same(terms.Version, latest.TermsVersion)));
        }

        if (current.Privacy is { } privacy)
        {
            list.Add(Describe(privacy, latest is null || !Same(privacy.Version, latest.PrivacyVersion)));
        }

        // BR2: with no earlier acceptance, one unreadable document is the server's problem and owes nothing.
        var owes = latest is null
            ? current.Terms is not null && current.Privacy is not null
            : list.Exists(document => document.Changed);

        return new LegalAcceptanceStatusResponse(owes, list);
    }

    /// <summary>
    /// Records the acceptance. Success is true when a row was written and false when nothing was owed (BR4).
    /// A user that does not exist is <see cref="ErrorKind.NotFound"/>.
    /// </summary>
    public async Task<Result<bool>> AcceptAsync(
        Guid userId,
        AcceptLegalDocumentsRequest request,
        string requestLocale,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Change note v2: the locale becomes a stored field and never selects a file; refuse anything that is not a shipped language.
        var storedLocale = SupportedLanguages.Canonical(request.Locale);
        if (storedLocale is null)
        {
            return Result.Failure<bool>(new Error(IdentityErrorCodes.LegalAcceptanceLocaleInvalid, ErrorKind.Validation));
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<bool>(new Error(IdentityErrorCodes.Forbidden, ErrorKind.NotFound));
        }

        var current = await documents.GetCurrentAsync(requestLocale, cancellationToken);
        if (current.Differs(request.TermsVersion, request.PrivacyVersion))
        {
            return Result.Failure<bool>(new Error(IdentityErrorCodes.LegalAcceptanceVersionOutdated, ErrorKind.Conflict));
        }

        return await consents.RunExclusiveForUserAsync(
            userId,
            async () =>
            {
                var latest = await consents.FindLatestAsync(userId, cancellationToken);

                // BR2: a document the server cannot read keeps the version of the last acceptance; with none, nothing is owed.
                var termsVersion = current.Terms?.Version ?? latest?.TermsVersion;
                var privacyVersion = current.Privacy?.Version ?? latest?.PrivacyVersion;
                if (termsVersion is null || privacyVersion is null)
                {
                    return Result.Success(false);
                }

                if (latest is not null && Same(termsVersion, latest.TermsVersion) && Same(privacyVersion, latest.PrivacyVersion))
                {
                    return Result.Success(false);
                }

                await consents.AddAsync(
                    new ConsentRecord
                    {
                        UserId = userId,
                        TermsVersion = termsVersion,
                        PrivacyVersion = privacyVersion,
                        DeclaresAdult = user.IsAdultDeclared,
                        Locale = storedLocale,
                        AcceptedAt = timeProvider.GetUtcNow(),
                        IpAddress = ipAddress,
                    },
                    cancellationToken);
                return Result.Success(true);
            },
            cancellationToken);
    }

    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);

    private static LegalAcceptanceDocumentResponse Describe(LegalDocumentResponse document, bool changed) =>
        new(document.Topic, document.Version, document.EffectiveDate, document.Title, document.IsPlaceholder, changed);
}
