namespace Simulab.Identity.Infrastructure;

/// <summary>F-84 BR1, BR2, BR6: which version of a certificate is the previous one.</summary>
public static class PreviousCertificateSelector
{
    public static PreviousCertificateSelection Select(IReadOnlyList<CertificateVersion> versions, string? currentValue)
    {
        ArgumentNullException.ThrowIfNull(versions);
        return new PreviousCertificateSelection(null, null);
    }
}
