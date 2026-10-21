using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.LegalAcceptance;

/// <summary>Reads the current terms and privacy documents of a locale in one go (sign-up and F-85 both need the pair).</summary>
public sealed class LegalDocumentReader(ILegalDocumentProvider legalDocuments)
{
    public async Task<CurrentLegalDocuments> GetCurrentAsync(string locale, CancellationToken cancellationToken = default)
    {
        var terms = await legalDocuments.GetCurrentAsync(LegalTopic.Terms, locale, cancellationToken);
        var privacy = await legalDocuments.GetCurrentAsync(LegalTopic.Privacy, locale, cancellationToken);
        return new CurrentLegalDocuments(terms, privacy);
    }
}
