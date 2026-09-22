namespace Simulab.Identity.Contracts;

/// <summary>The account itself in the data export (F-16 BR4).</summary>
public sealed record AccountDataResponse(
    Guid Id,
    string Email,
    string? FullName,
    string? PhoneNumber,
    string PreferredLanguage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EmailVerifiedAt,
    bool IsAdultDeclared,
    string Status);
