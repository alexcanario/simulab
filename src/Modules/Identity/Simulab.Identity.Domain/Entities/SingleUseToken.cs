using Simulab.SharedKernel.Entities;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// A link sent by email that works once (F-4 BR8, F-7 BR2). Only the hash is stored: the raw value exists
/// in the email and nowhere else, so a database dump does not let anyone use it.
/// </summary>
public abstract class SingleUseToken : TenantEntity
{
    public Guid UserId { get; init; }

    public required string TokenHash { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Set when the token is used or replaced by a newer one; a consumed token never works again.</summary>
    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt is not null;

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt <= now;

    /// <summary>Marks the token used. Consuming twice keeps the first instant.</summary>
    public void Consume(DateTimeOffset consumedAt) => ConsumedAt ??= consumedAt;
}
