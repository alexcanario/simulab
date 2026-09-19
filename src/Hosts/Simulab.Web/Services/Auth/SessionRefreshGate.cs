namespace Simulab.Web.Services.Auth;

/// <summary>
/// Keeps two refreshes of one web session from running at once (B-3, BR2): the refresh token is single
/// use (F-5 BR5), so the loser of a race would end a perfectly good session. A fixed set of striped
/// locks keeps memory bounded; two sessions sharing a stripe only wait for each other briefly.
/// In-process only: v1 runs one Web instance (decision, 2026-09-19).
/// </summary>
public sealed class SessionRefreshGate
{
    private const int Stripes = 64;

    private readonly SemaphoreSlim[] _locks = Enumerable.Range(0, Stripes).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task<IDisposable> EnterAsync(string webSessionId, CancellationToken cancellationToken = default)
    {
        var gate = _locks[(int)((uint)StringComparer.Ordinal.GetHashCode(webSessionId) % Stripes)];
        await gate.WaitAsync(cancellationToken);
        return new Release(gate);
    }

    private sealed class Release(SemaphoreSlim gate) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
