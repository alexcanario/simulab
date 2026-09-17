using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// The current version of an institutional document for one locale. Internal to the module: no other
/// module reads legal content today (profile: add structure on the second use).
/// </summary>
public interface ILegalDocumentProvider
{
    /// <summary>Null when there is no document for that topic and locale.</summary>
    Task<LegalDocumentResponse?> GetCurrentAsync(LegalTopic topic, string locale, CancellationToken cancellationToken = default);
}
