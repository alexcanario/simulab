using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Simulab.Email;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>
/// The whole Api with a database of its own, a recorded email sender and a clock the test moves. The
/// migrations run on start, so every test also proves the module's schema applies.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private string _connectionString = string.Empty;

    /// <summary>Every email the app tried to send, in order.</summary>
    public RecordingEmailSender Emails { get; } = new();

    /// <summary>Starts at a fixed instant so token lifetimes and cooldowns are exact.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Extra configuration a test class adds on top of the defaults.</summary>
    public Action<IWebHostBuilder>? ConfigureHost { get; set; }

    private string _redisConnectionString = string.Empty;

    /// <summary>Creates this host's own database and points it at the shared Redis container. Called before the first request.</summary>
    public async Task PrepareAsync(string name)
    {
        _connectionString = await PostgresServer.CreateDatabaseAsync(name);
        _redisConnectionString = await RedisServer.ConnectionStringAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("ConnectionStrings:simulab", _connectionString);
        builder.UseSetting("ConnectionStrings:mailpit", "smtp://localhost:1025");
        builder.UseSetting("ConnectionStrings:redis", _redisConnectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStart", "true");
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:FromAddress"] = "no-reply@simulab.app",
                ["Email:FromName"] = "Simulab",
                ["Identity:VerificationUrl"] = "https://localhost/verify-email",
                ["Identity:PasswordResetUrl"] = "https://localhost/reset-password",
                ["Identity:ForgotPasswordUrl"] = "https://localhost/forgot-password",
                ["Authentication:OpenIddict:ClientId"] = TestClient.ClientId,
                ["Authentication:OpenIddict:ClientSecret"] = TestClient.ClientSecret,
            }));

        ConfigureHost?.Invoke(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    /// <summary>A client whose calls carry one language, the way the Web sends the visitor's culture.</summary>
    public HttpClient CreateClient(string locale)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        return client;
    }

    public string ConnectionString => _connectionString;
}
