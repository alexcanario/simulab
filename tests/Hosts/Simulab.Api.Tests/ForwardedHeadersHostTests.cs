using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.Testing;

namespace Simulab.Api.Tests;

/// <summary>
/// F-64 AC4 through the real Api pipeline: behind the cloud ingress a request arrives as http, and OpenIddict refuses it
/// (ID2083) unless the host believes <c>X-Forwarded-Proto</c> from the proxy that sent it. An unlisted sender is never
/// believed (BR6).
/// </summary>
public sealed class ForwardedHeadersHostTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Proxy = "10.0.0.5";

    /// <summary>The test server has no network address; this gives each request the one a proxy would have.</summary>
    private sealed class FixedRemoteAddress(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    private async Task<string> TokenRequestAnswerAsync(string sender, bool proxyListed, bool sendsProto)
    {
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Staging");
            CloudSettings.Apply(builder);
            if (proxyListed)
            {
                builder.UseSetting("ForwardedHeaders:KnownProxies:0", Proxy);
            }

            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter>(new FixedRemoteAddress(IPAddress.Parse(sender))));
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "unsupported-grant" }),
        };
        if (sendsProto)
        {
            request.Headers.Add("X-Forwarded-Proto", "https");
        }

        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        using var response = await client.SendAsync(request);

        // The request is answered whatever it says: the question is only whether the transport check let it in.
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task ListedProxy_ForwardedHttps_OpenIddictTreatsTheRequestAsHttps()
    {
        var answer = await TokenRequestAnswerAsync(Proxy, proxyListed: true, sendsProto: true);

        answer.Should().NotContain("ID2083").And.Contain("error", "the request reached the protocol checks");
    }

    [Fact]
    public async Task ListedProxy_NoForwardedProto_OpenIddictRefusesPlainHttp()
    {
        var answer = await TokenRequestAnswerAsync(Proxy, proxyListed: true, sendsProto: false);

        answer.Should().Contain("ID2083", "a plain http request is refused when nobody vouches for https");
    }

    [Fact]
    public async Task UnlistedSender_ForwardedHttps_TheHeaderIsIgnored()
    {
        var answer = await TokenRequestAnswerAsync("198.51.100.20", proxyListed: true, sendsProto: true);

        answer.Should().Contain("ID2083");
    }

    [Fact]
    public async Task NoProxyListed_ForwardedHttps_TheHeaderIsIgnored()
    {
        var answer = await TokenRequestAnswerAsync(Proxy, proxyListed: false, sendsProto: true);

        answer.Should().Contain("ID2083");
    }

    private static async Task<List<RecordedLogEntry>> WarningsAfterAsync(WebApplicationFactory<Program> factory, string environment, bool proxyListed, int requests)
    {
        var logs = new RecordingLoggerProvider();
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudSettings.Apply(builder);
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
            if (proxyListed)
            {
                builder.UseSetting("ForwardedHeaders:KnownProxies:0", Proxy);
            }

            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter>(new FixedRemoteAddress(IPAddress.Parse("10.0.0.9"))));
        });
        using var client = host.CreateClient();

        for (var request = 0; request < requests; request++)
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/info");
            message.Headers.Add("X-Forwarded-For", "203.0.113.10");
            using var response = await client.SendAsync(message);
            response.StatusCode.Should().Be(HttpStatusCode.OK, "the request is answered whatever the log says");
        }

        return [.. logs.Entries.Where(entry => entry.Category == "TrustedProxies")];
    }

    /// <summary>F-64 D12: with no proxy listed the first forwarded header names the address it came from, once.</summary>
    [Fact]
    public async Task NoProxyListed_AForwardedHeaderIsLoggedOnceWithTheAddressItCameFrom()
    {
        var entries = await WarningsAfterAsync(factory, "Staging", proxyListed: false, requests: 3);

        entries.Should().ContainSingle().Which.Message.Should().Contain("10.0.0.9").And.Contain("KnownProxies");
    }

    [Theory]
    [InlineData("Staging", true)]
    [InlineData("Development", false)]
    public async Task ProxyListedOrDevelopment_NothingIsLogged(string environment, bool proxyListed)
    {
        // The Development host also needs no cloud settings, which Apply only adds: harmless there.
        var entries = await WarningsAfterAsync(factory, environment, proxyListed, requests: 1);

        entries.Should().BeEmpty();
    }
}
