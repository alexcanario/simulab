using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Jobs;
using Simulab.SharedKernel.Messaging;

namespace Simulab.Identity.Infrastructure.Account;

/// <summary>
/// Finishes an erasure that committed (F-59 BR3): revokes every session again (a no-op when the request
/// already did it, the backstop when the process died first), then publishes <see cref="UserErased"/>.
/// Safe to run more than once (BR4).
/// </summary>
public sealed class AccountErasedJobHandler(
    IRefreshSessionStore sessions,
    IIntegrationEventPublisher events) : IJobHandler
{
    public string Type => AccountErasedJob.Type;

    public async Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var (userId, erasedAt) = AccountErasedJob.Deserialize(payload);

        // F-10 BR13: a consumer only hears after the tokens stopped.
        await sessions.RevokeAllAsync(userId, cancellationToken: cancellationToken);
        await events.PublishAsync(new UserErased(userId, erasedAt), cancellationToken);
    }
}
