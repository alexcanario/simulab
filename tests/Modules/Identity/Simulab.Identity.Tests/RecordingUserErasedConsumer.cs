using System.Collections.Concurrent;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Messaging;

namespace Simulab.Identity.Tests;

/// <summary>
/// A real consumer of <see cref="UserErased"/>, registered in the test host the way a second module would
/// register its own (F-10, BR13). It only remembers what it was given.
/// </summary>
public sealed class RecordingUserErasedConsumer(ErasedAccounts erased) : IIntegrationEventConsumer<UserErased>
{
    public Task HandleAsync(UserErased integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        erased.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

/// <summary>What <see cref="RecordingUserErasedConsumer"/> saw, shared by the host and the test.</summary>
public sealed class ErasedAccounts
{
    private readonly ConcurrentQueue<UserErased> _events = new();

    public IReadOnlyList<UserErased> Events => [.. _events];

    public void Add(UserErased integrationEvent) => _events.Enqueue(integrationEvent);
}
