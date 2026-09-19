namespace Simulab.Identity.Contracts;

/// <summary>The signed-in user's own profile (F-8 UC1). The email is shown, never changed here (BR4).</summary>
public sealed record ProfileResponse(string Email, string? FullName, string PreferredLanguage);
