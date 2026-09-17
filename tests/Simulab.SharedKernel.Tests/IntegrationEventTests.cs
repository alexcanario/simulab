using Microsoft.Extensions.DependencyInjection;
using Simulab.SharedKernel.Messaging;

namespace Simulab.SharedKernel.Tests;

/// <summary>AC9: publishing reaches every consumer, in order, and a failure reaches the publisher.</summary>
public class IntegrationEventTests
{
    private sealed record AccountErased(Guid UserId) : IIntegrationEvent;

    private sealed class RecordingConsumer(List<string> log, string name) : IIntegrationEventConsumer<AccountErased>
    {
        public Task HandleAsync(AccountErased integrationEvent, CancellationToken cancellationToken = default)
        {
            log.Add(name);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingConsumer(List<string> log) : IIntegrationEventConsumer<AccountErased>
    {
        public Task HandleAsync(AccountErased integrationEvent, CancellationToken cancellationToken = default)
        {
            log.Add("failing");
            throw new InvalidOperationException("consumer failed");
        }
    }

    private static IIntegrationEventPublisher PublisherWith(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIntegrationEvents();
        register(services);
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
    }

    [Fact]
    public async Task Publish_TwoConsumers_RunsBothInRegistrationOrder()
    {
        var log = new List<string>();
        var publisher = PublisherWith(services =>
        {
            services.AddScoped<IIntegrationEventConsumer<AccountErased>>(_ => new RecordingConsumer(log, "first"));
            services.AddScoped<IIntegrationEventConsumer<AccountErased>>(_ => new RecordingConsumer(log, "second"));
        });

        await publisher.PublishAsync(new AccountErased(Guid.CreateVersion7()), CancellationToken.None);

        log.Should().Equal("first", "second");
    }

    [Fact]
    public async Task Publish_NoConsumer_Completes()
    {
        var publisher = PublisherWith(_ => { });

        var publish = async () => await publisher.PublishAsync(new AccountErased(Guid.CreateVersion7()), CancellationToken.None);

        await publish.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_FailingConsumer_ReachesTheCallerAndStops()
    {
        var log = new List<string>();
        var publisher = PublisherWith(services =>
        {
            services.AddScoped<IIntegrationEventConsumer<AccountErased>>(_ => new FailingConsumer(log));
            services.AddScoped<IIntegrationEventConsumer<AccountErased>>(_ => new RecordingConsumer(log, "after"));
        });

        var publish = async () => await publisher.PublishAsync(new AccountErased(Guid.CreateVersion7()), CancellationToken.None);

        (await publish.Should().ThrowAsync<InvalidOperationException>()).WithMessage("consumer failed");
        log.Should().Equal("failing");
    }

    [Fact]
    public void AddIntegrationEventConsumer_RegistersTheConsumerForItsEvent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIntegrationEvents();
        services.AddIntegrationEventConsumer<AccountErased, CountingConsumer>();

        using var scope = services.BuildServiceProvider().CreateScope();

        scope.ServiceProvider.GetServices<IIntegrationEventConsumer<AccountErased>>()
            .Should().ContainSingle().Which.Should().BeOfType<CountingConsumer>();
    }

    private sealed class CountingConsumer : IIntegrationEventConsumer<AccountErased>
    {
        public Task HandleAsync(AccountErased integrationEvent, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
