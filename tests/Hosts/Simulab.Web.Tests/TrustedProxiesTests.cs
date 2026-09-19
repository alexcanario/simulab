using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Simulab.Web.Services;

namespace Simulab.Web.Tests;

/// <summary>B-4 AC5: <c>X-Forwarded-For</c> is believed only from a proxy the configuration lists.</summary>
public sealed class TrustedProxiesTests
{
    private static async Task<(bool Enabled, string? Address)> VisitorAddressAsync(Dictionary<string, string?> settings, string connection, string forwardedFor)
    {
        var options = new ForwardedHeadersOptions();
        var enabled = TrustedProxies.Configure(options, new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(connection);
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        if (enabled)
        {
            var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
            await middleware.Invoke(context);
        }

        return (enabled, context.Connection.RemoteIpAddress?.ToString());
    }

    [Fact]
    public async Task ListedProxy_TheForwardedAddressIsTheVisitors()
    {
        var result = await VisitorAddressAsync(new() { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" }, "10.0.0.5", "203.0.113.10");

        result.Should().Be((true, "203.0.113.10"));
    }

    [Fact]
    public async Task ListedNetwork_TheForwardedAddressIsTheVisitors()
    {
        var result = await VisitorAddressAsync(new() { ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/16" }, "10.0.3.7", "203.0.113.10");

        result.Should().Be((true, "203.0.113.10"));
    }

    [Fact]
    public async Task UnlistedSender_TheHeaderIsIgnored()
    {
        var result = await VisitorAddressAsync(new() { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" }, "198.51.100.20", "203.0.113.10");

        result.Should().Be((true, "198.51.100.20"));
    }

    /// <summary>Dev: nothing listed, not even loopback (the framework's default), so the middleware is not added.</summary>
    [Fact]
    public async Task NothingListed_TheHeaderIsIgnored()
    {
        var result = await VisitorAddressAsync([], "127.0.0.1", "203.0.113.10");

        result.Should().Be((false, "127.0.0.1"));
    }
}
