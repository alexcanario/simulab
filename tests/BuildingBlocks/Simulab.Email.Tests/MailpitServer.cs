using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Email.Tests;

/// <summary>One Mailpit container for this test project: SMTP on 1025, HTTP API on 8025.</summary>
public sealed class MailpitServer : IAsyncLifetime
{
    private const int SmtpPort = 1025;
    private const int ApiPort = 8025;

    private readonly IContainer _container = new ContainerBuilder("axllent/mailpit:v1.29")
        .WithPortBinding(SmtpPort, true)
        .WithPortBinding(ApiPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(ApiPort).ForPath("/readyz")))
        .Build();

    public string Host => _container.Hostname;

    public int Smtp => _container.GetMappedPublicPort(SmtpPort);

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Every message Mailpit has captured, newest first.</summary>
    public async Task<IReadOnlyList<MailpitMessage>> MessagesAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri($"http://{Host}:{_container.GetMappedPublicPort(ApiPort)}") };
        var page = await client.GetFromJsonAsync<MailpitMessagePage>("/api/v1/messages", AppJson.Options, CancellationToken.None);
        return page?.Messages ?? [];
    }
}
