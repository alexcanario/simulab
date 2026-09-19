using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Simulab.Web.Services;

/// <summary>
/// B-4 BR5: the Web believes <c>X-Forwarded-For</c> only from the proxies its configuration lists
/// (<c>ForwardedHeaders:KnownProxies</c>, <c>ForwardedHeaders:KnownNetworks</c>). Both are empty in dev, so the
/// connection's address is the visitor's; the cloud environment fills them (<c>docs/infra.md</c>).
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

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
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

    private static InvalidOperationException Invalid(string key, string value) =>
        new($"The configuration '{SectionName}:{key}' has '{value}', which is not an IP address or network.");
}
