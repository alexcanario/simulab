using Simulab.SharedKernel.Entities;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// A single-use verification link (BR8). Only the hash is stored: the raw value exists in the email
/// and nowhere else, so a database dump does not let anyone activate an account.
/// </summary>
public class EmailVerificationToken : TenantEntity
{
    public Guid UserId { get; init; }

    public required string TokenHash { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Set when the token is used or replaced by a resend; a consumed token never verifies again.</summary>
    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt is not null;

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt <= now;

    /// <summary>Marks the token used. Consuming twice keeps the first instant.</summary>
    public void Consume(DateTimeOffset consumedAt) => ConsumedAt ??= consumedAt;
}
