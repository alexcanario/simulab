using Simulab.SharedKernel.Entities;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// What a user accepted, when, from where and in which language (BR6). Written once at sign-up and
/// never updated or deleted: it is the evidence LGPD and GDPR ask for.
/// </summary>
public class ConsentRecord : TenantEntity
{
    public Guid UserId { get; init; }

    /// <summary>The terms version shown and accepted, as the manifest names it.</summary>
    public required string TermsVersion { get; init; }

    /// <summary>The privacy policy version shown and accepted.</summary>
    public required string PrivacyVersion { get; init; }

    /// <summary>The 18+ self-declaration, in the same event.</summary>
    public bool DeclaresAdult { get; init; }

    /// <summary>The locale the documents were shown in, so the exact text can be found again.</summary>
    public required string Locale { get; init; }

    public DateTimeOffset AcceptedAt { get; init; }

    /// <summary>The client address, when the host knows it.</summary>
    public string? IpAddress { get; init; }
}
