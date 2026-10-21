namespace Simulab.Identity.Infrastructure;

/// <summary>
/// F-84 BR1: where the Api reads the versions of a vault certificate besides the current one. Registered by the Api host only
/// outside Development and only with a vault connection (BR9); without it a role has no previous certificate.
/// </summary>
public interface ICertificateVersionSource
{
    /// <summary>The newest versions of a vault secret, at most <paramref name="count"/>, in any order (the caller sorts them by creation date).</summary>
    Task<IReadOnlyList<CertificateVersion>> GetNewestAsync(string secretName, int count, CancellationToken cancellationToken);
}
