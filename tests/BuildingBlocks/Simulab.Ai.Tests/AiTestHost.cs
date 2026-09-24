using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Simulab.Ai.Persistence;
using Simulab.Persistence;
using Simulab.Plans.Contracts;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Ai.Tests;

/// <summary>
/// The gateway on a real PostgreSQL database of its own, wired the way the Api host wires it
/// (<see cref="AiServiceCollectionExtensions.AddAiGateway"/>), with a clock the test moves, a caller
/// the test chooses and the wire replaced by <see cref="StubAnthropicHandler"/>.
/// </summary>
public sealed class AiTestHost : IAsyncDisposable
{
    private ServiceProvider _services = null!;
    private string _connectionString = string.Empty;

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));

    public StubAnthropicHandler Anthropic { get; } = new();

    public TestCurrentUser User { get; } = new();

    public TestEntitlements Entitlements { get; } = new();

    public IAiGateway Gateway => _services.GetRequiredService<IAiGateway>();

    /// <summary>Starts the host. A null <paramref name="apiKey"/> is what "no key configured" means (BR4).</summary>
    public static async Task<AiTestHost> StartAsync(
        string name,
        string? apiKey = "test-key",
        IDictionary<string, string?>? settings = null)
    {
        var host = new AiTestHost();
        await host.InitializeAsync(name, apiKey, settings);
        return host;
    }

    public async Task<IReadOnlyList<AiCall>> CallsAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        return await context.Calls.AsNoTracking().OrderBy(call => call.StartedAt).ToListAsync();
    }

    /// <summary>Fills the month with calls already made, the way a user near the ceiling looks (BR3).</summary>
    public async Task SeedSuccessfulCallsAsync(Guid userId, int count)
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        for (var index = 0; index < count; index++)
        {
            context.Calls.Add(AiCall.ForAnswer(
                userId,
                AiPurposes.Diagnostics,
                "claude-opus-5",
                inputTokens: 10,
                outputTokens: 10,
                new AiModelPrice(),
                TimeSpan.FromMilliseconds(1),
                Clock.GetUtcNow()));
        }

        await context.SaveChangesAsync();
    }

    private async Task InitializeAsync(string name, string? apiKey, IDictionary<string, string?>? settings)
    {
        _connectionString = await PostgresServer.CreateDatabaseAsync(name);
        _services = BuildServices(_connectionString, apiKey, settings);
        await _services.MigrateAiAsync();
    }

    private ServiceProvider BuildServices(string connectionString, string? apiKey, IDictionary<string, string?>? settings)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Ai:DefaultModel"] = "claude-opus-5",
            ["Ai:Prices:claude-opus-5:InputPerMillion"] = "5.0",
            ["Ai:Prices:claude-opus-5:OutputPerMillion"] = "25.0"
        };

        if (apiKey is not null)
        {
            values["Ai:ApiKey"] = apiKey;
        }

        if (settings is not null)
        {
            foreach (var (key, value) in settings)
            {
                values[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Critical));
        services.AddModulePersistence();
        services.AddSingleton<ICurrentUser>(User);
        services.AddAiGateway(configuration, connectionString);

        // The wire, replaced. Everything above it is the production path.
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            return new AnthropicClient
            {
                ApiKey = options.ApiKey ?? string.Empty,
                HttpClient = new HttpClient(Anthropic),
                MaxRetries = 0
            };
        });
        services.AddSingleton<IEntitlementService>(Entitlements);

        return services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();

        // EF pools connections per connection string for the whole process (profile: test strategy).
        using var connection = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(connection);
    }
}

/// <summary>The caller the test chooses. Null is an anonymous visitor (BR2).</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; } = Guid.CreateVersion7();
}

/// <summary>The plan the test gives the caller. A null limit is no ceiling (BR3).</summary>
public sealed class TestEntitlements : IEntitlementService
{
    public int? Limit { get; set; }

    public Task<bool> HasFeatureAsync(Guid userId, string feature, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<int?> GetLimitAsync(Guid userId, string feature, CancellationToken cancellationToken = default) =>
        Task.FromResult(Limit);
}
