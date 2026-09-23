namespace Simulab.Web.Services.Auth;

/// <summary>
/// The Web host's side of F-20: the switch (BR1, the same <c>Identity:GoogleSignInEnabled</c> the Api reads) and the
/// OAuth client registered in Google Cloud. Read once at start: turning the feature on or off needs a restart, as on the Api.
/// </summary>
public sealed class GoogleSignInSettings
{
    /// <summary>The OpenID Connect scheme that goes to Google and back.</summary>
    public const string Scheme = "Google";

    /// <summary>The short-lived cookie that holds Google's answer between the callback and <c>/account/google/complete</c>.</summary>
    public const string ExternalScheme = "Google.External";

    /// <summary>Google's issuer; its discovery document gives every endpoint.</summary>
    public const string Authority = "https://accounts.google.com";

    /// <summary>The redirect URI registered in Google Cloud, on this host.</summary>
    public const string CallbackPath = "/signin-google";

    public bool Enabled { get; init; }

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public static GoogleSignInSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = new GoogleSignInSettings
        {
            Enabled = configuration.GetValue("Identity:GoogleSignInEnabled", false),
            ClientId = configuration["Authentication:Google:ClientId"],
            ClientSecret = configuration["Authentication:Google:ClientSecret"],
        };

        // A switched-off feature needs no client, so turning it off never fails a start.
        if (settings.Enabled && (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ClientSecret)))
        {
            throw new InvalidOperationException(
                "Authentication:Google:ClientId and Authentication:Google:ClientSecret are required while Identity:GoogleSignInEnabled is true.");
        }

        return settings;
    }
}
