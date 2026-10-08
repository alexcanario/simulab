using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Simulab.ServiceDefaults;

/// <summary>
/// B-4 BR5, F-64 BR6: each host believes <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> only from the proxies
/// its configuration lists (<c>ForwardedHeaders:KnownProxies</c>, <c>ForwardedHeaders:KnownNetworks</c>). Both are
/// empty in dev, so the connection is the visitor's; the cloud environment fills them (<c>docs/infra.md</c>). The
/// scheme matters behind a TLS-ending ingress: OpenIddict refuses a request it sees as plain http.
/// </summary>
public static class TrustedProxies
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Fills <paramref name="options"/> from configuration and says whether any proxy is trusted. The framework's
    /// default (loopback) is dropped: a header from an unlisted address is never believed.
    /// </summary>
    public static bool Configure(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        var section = configuration.GetSection(SectionName);
        foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? [])
        {
            options.KnownProxies.Add(IPAddress.TryParse(proxy, out var address)
                ? address
                : throw Invalid("KnownProxies", proxy));
        }

        foreach (var network in section.GetSection("KnownNetworks").Get<string[]>() ?? [])
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.TryParse(network, out var range)
                ? range
                : throw Invalid("KnownNetworks", network));
        }

        return options.KnownProxies.Count > 0 || options.KnownIPNetworks.Count > 0;
    }

    /// <summary>
    /// F-64 D12: while no proxy is listed, the first request that carries a forwarded header is logged once with the address it
    /// came from, so the owner reads the ingress address on the first staging and writes it into the configuration
    /// (<c>docs/infra.md</c>) instead of guessing a range. The header is still ignored.
    /// </summary>
    public static IApplicationBuilder UseUnlistedProxyLog(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var logged = 0;
        return app.Use((context, next) =>
        {
            var headers = context.Request.Headers;
            if ((headers.ContainsKey("X-Forwarded-For") || headers.ContainsKey("X-Forwarded-Proto"))
                && Interlocked.Exchange(ref logged, 1) == 0)
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(TrustedProxies)).LogWarning(
                    "A forwarded header arrived from {Address} and is ignored: {Section}:KnownProxies and {Section}:KnownNetworks list no proxy.",
                    context.Connection.RemoteIpAddress,
                    SectionName,
                    SectionName);
            }

            return next(context);
        });
    }

    private const int MaxLoggedSenders = 16;

    /// <summary>
    /// F-64 D12, owner's choice on 2026-10-08: with proxies listed, a forwarded header from an address that is NOT listed is
    /// also logged, once per address (at most <see cref="MaxLoggedSenders"/>), so the owner reads the ingress address the
    /// host really sees (the Api's internal ingress is not the Web's) instead of guessing. The header is still ignored.
    /// Registered before <c>UseForwardedHeaders</c>, which would otherwise consume the sender's address.
    /// </summary>
    public static IApplicationBuilder UseUnlistedSenderLog(this IApplicationBuilder app, ForwardedHeadersOptions listed)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(listed);

        var seen = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
        return app.Use((context, next) =>
        {
            var headers = context.Request.Headers;
            var sender = context.Connection.RemoteIpAddress;
            if (sender is not null
                && (headers.ContainsKey("X-Forwarded-For") || headers.ContainsKey("X-Forwarded-Proto"))
                && !IsListed(listed, sender)
                && seen.Count < MaxLoggedSenders
                && seen.TryAdd(sender.ToString(), 0))
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(TrustedProxies)).LogWarning(
                    "A forwarded header arrived from {Address}, which is not in {Section}:KnownProxies or {Section}:KnownNetworks, and is ignored: if it is the cloud ingress, list it.",
                    sender,
                    SectionName,
                    SectionName);
            }

            return next(context);
        });
    }

    /// <summary>The framework compares an IPv4 address that arrived as <c>::ffff:a.b.c.d</c> in both forms; so does this.</summary>
    private static bool IsListed(ForwardedHeadersOptions options, IPAddress sender)
    {
        var candidates = sender.IsIPv4MappedToIPv6 ? new[] { sender, sender.MapToIPv4() } : new[] { sender };
        return candidates.Any(candidate =>
            options.KnownProxies.Contains(candidate) || options.KnownIPNetworks.Any(network => network.Contains(candidate)));
    }

    private static InvalidOperationException Invalid(string key, string value) =>
        new($"The configuration '{SectionName}:{key}' has '{value}', which is not an IP address or network.");
}
