namespace Simulab.Identity.Contracts;

/// <summary>Chooses a new password with the token from the reset email (F-7 UC2).</summary>
public sealed record ResetPasswordRequest(string? Token, string? NewPassword);
