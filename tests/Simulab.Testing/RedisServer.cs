using Testcontainers.Redis;

namespace Simulab.Testing;

/// <summary>One Redis container per test project (profile: test strategy), started on first use.</summary>
public static class RedisServer
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static RedisContainer? _container;

    public static async Task<string> ConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (_container is not null)
        {
            return _container.GetConnectionString();
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (_container is null)
            {
                var container = new RedisBuilder("redis:7-alpine").Build();
                await container.StartAsync(cancellationToken);
                _container = container;
            }
        }
        finally
        {
            Gate.Release();
        }

        return _container.GetConnectionString();
    }
}
