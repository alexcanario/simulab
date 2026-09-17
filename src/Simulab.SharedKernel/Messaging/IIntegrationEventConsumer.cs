namespace Simulab.SharedKernel.Messaging;

/// <summary>Consumes an event of another module. Registered by the module that reacts to it.</summary>
public interface IIntegrationEventConsumer<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
