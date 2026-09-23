namespace Simulab.Web.Services.Auth;

/// <summary>
/// A first Google sign-in waiting for the confirmation page (F-20 BR8). The ID token stays on this server; the page
/// gets only the address and the name.
/// </summary>
public sealed record GoogleSignUpTicket(string IdToken, string Email, string? Name);
