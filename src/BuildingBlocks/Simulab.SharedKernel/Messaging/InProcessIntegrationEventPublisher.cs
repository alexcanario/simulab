using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Simulab.SharedKernel.Messaging;

/// <summary>
/// Runs every handler registered for the event type, in registration order, in the caller's scope.
/// A consumer failure is logged and reaches the caller. No consumer is not an error.
/// Replace it with a job-table implementation when delivery must survive a crash.
/// </summary>
public sealed class InProcessIntegrationEventPublisher(
    IServiceProvider services,
    ILogger<InProcessIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        foreach (var consumer in services.GetServices<IIntegrationEventConsumer<TEvent>>())
        {
            try
            {
                await consumer.HandleAsync(integrationEvent, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Consumer {Consumer} failed for {Event}", consumer.GetType().Name, typeof(TEvent).Name);
                throw;
            }
        }
    }
}
