namespace Simulab.Identity.Domain.Entities;

/// <summary>A single-use verification link, valid 24 hours (F-4 BR8).</summary>
public class EmailVerificationToken : SingleUseToken;
