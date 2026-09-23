namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>
/// The Google sign-in switch (F-20 BR1), in the <c>Identity</c> configuration section, next to <c>TotpEnabled</c>.
/// Off by default: ADR-0001 #13 ships the code switched off.
/// </summary>
public sealed class GoogleSignInOptions
{
    public const string SectionName = "Identity";

    /// <summary><c>Identity:GoogleSignInEnabled</c>. False: no grant, no registration route.</summary>
    public bool GoogleSignInEnabled { get; set; }
}
