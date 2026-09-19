namespace Simulab.Identity.Contracts;

/// <summary>Asks whether a reset link still works, without using it (F-7, decision: the page checks on load).</summary>
public sealed record PasswordResetTokenCheckRequest(string? Token);
