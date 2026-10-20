using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Simulab.Identity.Infrastructure;

/// <summary>
/// F-84: the previous signing and encryption certificates, read once at start from the version immediately before the
/// current one in the vault. They only validate and decrypt; the current ones sign and encrypt. A previous certificate that
/// cannot do its job, or a listing that fails, never stops the start: the role works with its current certificate and the
/// log says why (the name of the vault certificate and the reason, never its value).
/// </summary>
public sealed class PreviousOpenIddictCertificates
{
    /// <summary>A replica whose clock runs a little behind the vault's must not refuse a certificate created a moment ago.</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    private PreviousOpenIddictCertificates(X509Certificate2? signing, X509Certificate2? encryption)
    {
        Signing = signing;
        Encryption = encryption;
    }

    /// <summary>A role with no previous certificate: Development, no vault, or nothing to load.</summary>
    public static PreviousOpenIddictCertificates None { get; } = new(null, null);

    public X509Certificate2? Signing { get; }

    public X509Certificate2? Encryption { get; }

    public static PreviousOpenIddictCertificates Load(
        IConfiguration configuration,
        ICertificateVersionSource? source,
        PreviousCertificateOptions options,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (source is null)
        {
            return None;
        }

        // One bound for both roles (BR5): a vault that does not answer costs the start this long, once.
        using var deadline = new CancellationTokenSource(options.ListTimeout);
        var now = TimeProvider.System.GetUtcNow();
        return new PreviousOpenIddictCertificates(
            LoadRole(configuration, source, OpenIddictCertificates.SigningKey, X509KeyUsageFlags.DigitalSignature, now, deadline.Token, logger),
            LoadRole(configuration, source, OpenIddictCertificates.EncryptionKey, X509KeyUsageFlags.KeyEncipherment, now, deadline.Token, logger));
    }

    private static X509Certificate2? LoadRole(
        IConfiguration configuration,
        ICertificateVersionSource source,
        string key,
        X509KeyUsageFlags usage,
        DateTimeOffset now,
        CancellationToken deadline,
        ILogger logger)
    {
        // The vault names a certificate with "--" where the configuration key has ":".
        var name = key.Replace(":", "--", StringComparison.Ordinal);
        var current = configuration[key];

        IReadOnlyList<CertificateVersion> versions;
        try
        {
            deadline.ThrowIfCancellationRequested();
            versions = source.GetNewestAsync(name, count: 2, deadline).WaitAsync(deadline).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            Ignored(logger, name, "the versions could not be listed (timed out)");
            return null;
        }
        catch (Exception exception)
        {
            // Only the type: the message of a client exception is outside this code and could hold anything.
            Ignored(logger, name, $"the versions could not be listed ({exception.GetType().Name})");
            return null;
        }

        var selection = PreviousCertificateSelector.Select(versions, current);
        if (selection.Value is null)
        {
            if (selection.Problem is not null)
            {
                Ignored(logger, name, selection.Problem);
            }

            return null;
        }

        var (certificate, problem) = Inspect(selection.Value, current!, usage, now);
        if (problem is not null)
        {
            Ignored(logger, name, problem);
        }

        return certificate;
    }

    private static (X509Certificate2? Certificate, string? Problem) Inspect(string value, string current, X509KeyUsageFlags usage, DateTimeOffset now)
    {
        X509Certificate2 certificate;
        try
        {
            certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(value.Trim()), password: null, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return (null, "it is not the base64 of a PKCS#12 file without a password");
        }

        var problem = Problem(certificate, usage, now, current);
        if (problem is null)
        {
            return (certificate, null);
        }

        certificate.Dispose();
        return (null, problem);
    }

    private static string? Problem(X509Certificate2 certificate, X509KeyUsageFlags usage, DateTimeOffset now, string current)
    {
        if (!certificate.HasPrivateKey)
        {
            return "it has no private key";
        }

        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
        if (keyUsage is null || !keyUsage.KeyUsages.HasFlag(usage))
        {
            return "it lacks the key usage that its role needs";
        }

        if (now + ClockSkew < certificate.NotBefore)
        {
            return "it is not valid yet";
        }

        if (now > certificate.NotAfter)
        {
            return $"it expired on {certificate.NotAfter.ToUniversalTime():yyyy-MM-dd} UTC";
        }

        // If the validity of the policy ever got shorter, an old certificate that outlives the current one would be
        // picked by OpenIddict as the one that signs.
        using var currentCertificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(current.Trim()), password: null, X509KeyStorageFlags.EphemeralKeySet);
        return certificate.NotAfter >= currentCertificate.NotAfter ? "it does not expire earlier than the current certificate" : null;
    }

    private static void Ignored(ILogger logger, string name, string reason) =>
        logger.LogWarning("OpenIddict previous certificate '{Certificate}' ignored: {Reason}", name, reason);
}
