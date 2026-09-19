namespace Simulab.Identity.Domain.Entities;

/// <summary>A single-use link to choose a new password, valid 1 hour (F-7 BR2).</summary>
public class PasswordResetToken : SingleUseToken;
