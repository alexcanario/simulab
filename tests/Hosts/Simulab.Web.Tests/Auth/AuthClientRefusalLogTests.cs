using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulab.Testing;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// F-64 (2026-10-08): the token endpoint's 401 means the Api refused this host's own client credentials; the sign-in page only
/// shows "something went wrong", so the OAuth error must reach the log.
/// </summary>
public sealed class AuthClientRefusalLogTests
{
    private sealed class Answer(HttpStatusCode status, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }

    private static async Task<IReadOnlyList<RecordedLogEntry>> SignInAsync(HttpStatusCode status, string json)
    {
        var logs = new RecordingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        using var http = new HttpClient(new Answer(status, json)) { BaseAddress = new Uri("https://api.test") };
        var client = new AuthClient(http, Options.Create(new OpenIddictClientOptions { ClientSecret = "web-secret" }), factory.CreateLogger<AuthClient>());

        await client.SignInAsync("someone@example.com", "a-password");

        return logs.Entries;
    }

    [Fact]
    public async Task Unauthorized_LogsTheOauthErrorAndNeverTheSecret()
    {
        var entries = await SignInAsync(HttpStatusCode.Unauthorized, """{"error":"invalid_client","error_description":"The specified client credentials are invalid."}""");

        var entry = entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("invalid_client").And.NotContain("web-secret").And.NotContain("a-password");
    }

    [Fact]
    public async Task WrongPassword_IsNotLoggedAsAClientRefusal()
    {
        var entries = await SignInAsync(HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"The username/password couple is invalid."}""");

        entries.Should().BeEmpty();
    }
}
