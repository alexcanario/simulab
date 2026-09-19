namespace Simulab.Identity.Contracts;

/// <summary>
/// The account's length limits: the width of their columns, read by the Api (the authority), the EF mapping
/// and the Web (comfort checks). One number per limit (F-8 BR2, B-7 BR4).
/// </summary>
public static class AccountLimits
{
    /// <summary>A longer display name is refused, not cut.</summary>
    public const int FullNameMaxLength = 120;

    /// <summary>The longest valid address (RFC 5321); the email and user-name columns.</summary>
    public const int EmailMaxLength = 254;
}
