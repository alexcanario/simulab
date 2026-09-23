namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>The OAuth client registered in Google Cloud (F-20), in <c>Authentication:Google</c>. The Api needs only its id.</summary>
public sealed class GoogleClientOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>The audience every accepted ID token must carry (BR2).</summary>
    public string? ClientId { get; set; }

    /// <summary>Why these options cannot start the Api, or null when they can. A switched-off feature needs no client.</summary>
    public string? Problem(bool googleSignInEnabled) =>
        googleSignInEnabled && string.IsNullOrWhiteSpace(ClientId)
            ? "Authentication:Google:ClientId is required while Identity:GoogleSignInEnabled is true."
            : null;
}
