namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>What a Google ID token says, once its issuer, audience, signature and lifetime were checked (F-20 BR2).</summary>
/// <param name="Subject">Google's stable id for the account (<c>sub</c>); the key of the link in <c>user_logins</c>.</param>
/// <param name="Email">The Google address.</param>
/// <param name="EmailVerified">Whether Google marks the address as verified (<c>email_verified</c>).</param>
/// <param name="Name">The name on the Google account, when it has one.</param>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name);
