using System.Security.Cryptography.X509Certificates;

namespace Simulab.Identity.Infrastructure;

/// <summary>F-84 BR11: the certificates this Api signs and encrypts with, kept so the start can log which ones are trusted.</summary>
internal sealed record CurrentOpenIddictCertificates(X509Certificate2 Signing, X509Certificate2 Encryption);
