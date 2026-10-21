namespace Simulab.Identity.Infrastructure;

/// <summary>F-84: the outcome of choosing the previous version of a certificate: its value, or why there is none to report.</summary>
/// <param name="Value">The base64 of the previous certificate, or null when the role has none.</param>
/// <param name="Problem">A reason worth a warning (the listing disagrees with the current value); null for the quiet cases (one version, previous disabled).</param>
public sealed record PreviousCertificateSelection(string? Value, string? Problem);
