namespace Simulab.Identity.Contracts;

/// <summary>
/// A signed-in user downloads their own data (F-16 UC2). The account is the token subject (BR1), so the body
/// carries only the proof that the person at the keyboard owns it (BR2).
/// </summary>
public sealed record DataExportRequest(string? CurrentPassword);
