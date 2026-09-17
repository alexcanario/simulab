namespace Simulab.Identity.Contracts;

/// <summary>The result of consuming a verification token.</summary>
public sealed record VerifyEmailResponse(VerificationOutcome Outcome);
