namespace Simulab.Identity.Contracts;

/// <summary>The raw token from the verification link. It is posted, never sent as a query string to the API.</summary>
public sealed record VerifyEmailRequest(string Token);
