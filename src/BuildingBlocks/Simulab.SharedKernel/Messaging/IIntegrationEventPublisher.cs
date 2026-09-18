namespace Simulab.SharedKernel.Messaging;

/// <summary>
/// Publishes an event to the consumers registered in this process. Publishing is synchronous:
/// consumers run in sequence, in the caller's scope, and a failure reaches the caller.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
