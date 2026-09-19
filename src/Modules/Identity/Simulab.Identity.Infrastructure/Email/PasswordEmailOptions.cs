namespace Simulab.Identity.Infrastructure.Email;

/// <summary>Where the password emails point (F-7 BR4, BR11). Bound from configuration section <c>Identity</c>.</summary>
public sealed class PasswordEmailOptions
{
    public const string SectionName = "Identity";

    /// <summary>Absolute address of the Web page that uses the reset token, without the query string. The app host fills it in development.</summary>
    public string PasswordResetUrl { get; set; } = "https://localhost:7125/reset-password";

    /// <summary>Absolute address of the page to ask for a reset link, offered in the "password changed" email.</summary>
    public string ForgotPasswordUrl { get; set; } = "https://localhost:7125/forgot-password";
}
