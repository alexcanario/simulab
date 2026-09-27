namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// One commit for every write a handler stages under it (F-30 BR8). Disposing without a call to
/// <see cref="CommitAsync"/> rolls the writes back and clears the change tracker, so a handler that
/// returns early on a failure never needs its own rollback call.
/// </summary>
public interface IIdentityTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
