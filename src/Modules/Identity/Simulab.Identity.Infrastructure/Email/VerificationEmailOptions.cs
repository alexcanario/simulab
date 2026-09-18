namespace Simulab.Identity.Infrastructure.Email;

/// <summary>Where the verification link points. Bound from configuration section <c>Identity</c>.</summary>
public sealed class VerificationEmailOptions
{
    public const string SectionName = "Identity";

    /// <summary>
    /// Absolute address of the Web page that consumes the token, without the query string
    /// (for example <c>https://localhost:7125/verify-email</c>). The app host fills it in development.
    /// </summary>
    public string VerificationUrl { get; set; } = "https://localhost:7125/verify-email";
}
