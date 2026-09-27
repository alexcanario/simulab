namespace Simulab.Identity.Application.Abstractions;

/// <summary>The one of the sign-up transaction's two unique indexes a write collided with (F-30 BR6).</summary>
public enum IdentityUniqueViolation
{
    /// <summary>An email inserted at the same address after the handler's own lookup.</summary>
    UserEmail,

    /// <summary>A Google subject linked to another account after the handler's own lookup.</summary>
    GoogleLogin,
}
