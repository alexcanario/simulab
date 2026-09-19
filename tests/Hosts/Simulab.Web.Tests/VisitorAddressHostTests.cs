using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Services;

namespace Simulab.Web.Tests;

/// <summary>
/// B-4 AC4 through the real Web pipeline: the first request of a page is where the visitor's address is read
/// (App), and it reaches the service the Api client sends it from (Routes → <see cref="VisitorContext"/>).
/// </summary>
public sealed class VisitorAddressHostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>The test server has no network address; this gives each request the one a browser would have.</summary>
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

    [Fact]
    public async Task PageRequest_HandsTheVisitorsAddressToTheApiClient()
    {
        // One instance for the whole host, so the test can read what the page's scope stored.
        var visitor = new VisitorContext();
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(visitor);
            services.AddSingleton<IStartupFilter>(new FixedRemoteAddress(IPAddress.Parse("203.0.113.10")));
        }));

        using var response = await host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") })
            .GetAsync("/forgot-password");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        visitor.Address.Should().Be("203.0.113.10");
    }
}
