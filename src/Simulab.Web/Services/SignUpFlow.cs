namespace Simulab.Web.Services;

/// <summary>
/// Carries the address from sign-up to the "check your email" page. It is scoped to the circuit, so the
/// address never travels in a URL or a log (F-4, screen decision).
/// </summary>
public sealed class SignUpFlow
{
    public string? PendingEmail { get; set; }
}
