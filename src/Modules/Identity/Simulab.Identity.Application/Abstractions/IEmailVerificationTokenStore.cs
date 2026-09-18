using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>Reading and writing verification tokens. The application never sees a DbContext.</summary>
public interface IEmailVerificationTokenStore
{
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    /// <summary>The token with this hash, whatever its state; null when no token matches.</summary>
    Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Marks every token of this user consumed, so a resend invalidates the older links (BR11).</summary>
    Task ConsumePendingForUserAsync(Guid userId, DateTimeOffset consumedAt, CancellationToken cancellationToken = default);

    /// <summary>How many tokens were created for this user since the given instant; the per-address limits use it (BR12).</summary>
    Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>When the newest token of this user was created; null when the user has none.</summary>
    Task<DateTimeOffset?> LastCreatedAtAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ConsumeAsync(EmailVerificationToken token, DateTimeOffset consumedAt, CancellationToken cancellationToken = default);
}
