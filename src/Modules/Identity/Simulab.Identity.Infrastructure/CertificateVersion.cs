namespace Simulab.Identity.Infrastructure;

/// <summary>
/// F-84: one version of an OpenIddict certificate in the vault. <see cref="Value"/> is the base64 of the PKCS#12 file and is
/// null for a disabled version (the vault will not hand its value over).
/// </summary>
public sealed record CertificateVersion(string Id, DateTimeOffset CreatedOn, bool Enabled, string? Value);
