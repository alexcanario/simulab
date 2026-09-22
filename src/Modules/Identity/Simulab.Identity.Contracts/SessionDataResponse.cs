namespace Simulab.Identity.Contracts;

/// <summary>The user's sessions in the data export (F-16 BR4): a count only, since a session keeps no creation time.</summary>
public sealed record SessionDataResponse(int Active);
