using Microsoft.Extensions.DependencyInjection;

namespace Simulab.SharedKernel.Messaging;

public static class IntegrationEventServiceCollectionExtensions
{
    /// <summary>Registers the in-process publisher. Modules add their consumers with <see cref="AddIntegrationEventConsumer{TEvent, TConsumer}"/>.</summary>
    public static IServiceCollection AddIntegrationEvents(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventPublisher, InProcessIntegrationEventPublisher>();
        return services;
    }

    public static IServiceCollection AddIntegrationEventConsumer<TEvent, TConsumer>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        services.AddScoped<IIntegrationEventConsumer<TEvent>, TConsumer>();
        return services;
    }
}
