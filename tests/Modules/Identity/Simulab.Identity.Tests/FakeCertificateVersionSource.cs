using Simulab.Identity.Infrastructure;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>F-84: the vault's list of certificate versions, as a test writes it. Counts its calls so a test can prove it was never asked.</summary>
public sealed class FakeCertificateVersionSource : ICertificateVersionSource
{
    internal const string SigningName = "OpenIddict--SigningCertificate";
    internal const string EncryptionName = "OpenIddict--EncryptionCertificate";

    private static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Dictionary<string, IReadOnlyList<CertificateVersion>> _versions = [];

    public Func<Task<IReadOnlyList<CertificateVersion>>>? Behavior { get; set; }

    public int Calls { get; private set; }

    /// <summary>The current pair as the newest version, and the previous pair, when there is one, as the version before it.</summary>
    public static FakeCertificateVersionSource Of(TestCertificates current, TestCertificates? previous = null)
    {
        var source = new FakeCertificateVersionSource();
        source._versions[SigningName] = Versions(current.Signing, previous?.Signing);
        source._versions[EncryptionName] = Versions(current.Encryption, previous?.Encryption);
        return source;
    }

    /// <summary>Sets the versions of one secret exactly, newest last or in any order.</summary>
    public FakeCertificateVersionSource With(string secretName, params CertificateVersion[] versions)
    {
        _versions[secretName] = versions;
        return this;
    }

    public static CertificateVersion Enabled(string id, int day, string value) => new(id, Now.AddDays(day), true, value);

    public static CertificateVersion Disabled(string id, int day) => new(id, Now.AddDays(day), false, null);

    public async Task<IReadOnlyList<CertificateVersion>> GetNewestAsync(string secretName, int count, CancellationToken cancellationToken)
    {
        Calls++;
        if (Behavior is { } behavior)
        {
            return await behavior();
        }

        return _versions.TryGetValue(secretName, out var versions) ? versions : [];
    }

    private static List<CertificateVersion> Versions(string current, string? previous)
    {
        var list = new List<CertificateVersion> { Enabled("v2", 2, current) };
        if (previous is not null)
        {
            list.Add(Enabled("v1", 1, previous));
        }

        return list;
    }
}
