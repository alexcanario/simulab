using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>Reading and writing password reset tokens (F-7). The application never sees a DbContext.</summary>
public interface IPasswordResetTokenStore
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>The token with this hash, whatever its state; null when no token matches.</summary>
    Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Marks every unused token of this user consumed, so only the newest link works (BR1).</summary>
    Task ConsumePendingForUserAsync(Guid userId, DateTimeOffset consumedAt, CancellationToken cancellationToken = default);

    /// <summary>How many tokens were created for this user since the given instant; the per-account limit uses it (BR3).</summary>
    Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>When the newest token of this user was created; null when the user has none.</summary>
    Task<DateTimeOffset?> LastCreatedAtAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ConsumeAsync(PasswordResetToken token, DateTimeOffset consumedAt, CancellationToken cancellationToken = default);
}
