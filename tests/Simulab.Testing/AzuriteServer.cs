using Testcontainers.Azurite;

namespace Simulab.Testing;

/// <summary>One Azurite (blob emulator) container per test project (profile: test strategy), started on first use.</summary>
public static class AzuriteServer
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static AzuriteContainer? _container;

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
                // The newest Azure.Storage.Blobs sends an API version older Azurite releases refuse.
                var container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0")
                    .WithCommand("--skipApiVersionCheck")
                    .Build();
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
