namespace Simulab.Identity.Contracts;

/// <summary>A signed-in user changes the password with the current one (F-7 UC6).</summary>
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
