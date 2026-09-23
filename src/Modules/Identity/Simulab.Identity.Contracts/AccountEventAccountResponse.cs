namespace Simulab.Identity.Contracts;

/// <summary>The account an event is about (F-21, BR12). <paramref name="Email"/> is null for an erased account.</summary>
public sealed record AccountEventAccountResponse(Guid Id, string? Email);
