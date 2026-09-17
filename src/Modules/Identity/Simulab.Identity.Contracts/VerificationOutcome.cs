namespace Simulab.Identity.Contracts;

/// <summary>What happened when a verification token was posted.</summary>
public enum VerificationOutcome
{
    /// <summary>The account is active. Also returned when it already was (BR10).</summary>
    Verified,

    /// <summary>Unknown or already consumed token.</summary>
    Invalid,

    /// <summary>The token existed but is older than its lifetime; the page offers a resend.</summary>
    Expired
}
