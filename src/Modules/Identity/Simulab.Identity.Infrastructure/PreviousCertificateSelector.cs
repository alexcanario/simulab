namespace Simulab.Identity.Infrastructure;

/// <summary>F-84 BR1, BR2, BR6: which version of a certificate is the previous one.</summary>
public static class PreviousCertificateSelector
{
    public static PreviousCertificateSelection Select(IReadOnlyList<CertificateVersion> versions, string? currentValue)
    {
        ArgumentNullException.ThrowIfNull(versions);

        var newestFirst = versions.OrderByDescending(version => version.CreatedOn).ToList();
        if (newestFirst.Count < 2)
        {
            return new PreviousCertificateSelection(null, null);
        }

        // The current value came from the configuration provider in an earlier read; a renewal between the two reads would
        // make the version before the newest one the current one, and trusting it as the previous one would be wrong.
        var newest = newestFirst[0];
        if (newest.Value is null || !string.Equals(newest.Value.Trim(), currentValue?.Trim(), StringComparison.Ordinal))
        {
            return new PreviousCertificateSelection(
                null,
                "the newest version listed in the vault is not the current value this Api read (a renewal landed in between?)");
        }

        // Only the version immediately before the current one; an older enabled version is never promoted in its place.
        var previous = newestFirst[1];
        return previous is { Enabled: true, Value: { } value }
            ? new PreviousCertificateSelection(value, null)
            : new PreviousCertificateSelection(null, null);
    }
}
