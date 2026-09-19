using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Api;

/// <summary>
/// The address of the client a request is for (B-4). The Web calls the Api from its own server, so the
/// connection's address is the Web's; the Web sends the visitor's address in
/// <see cref="ClientAddressHeaders.Address"/> with its client secret, and only then is the header believed.
/// Anything else — no secret, a wrong one, a value that is not an IP address — falls back to the connection.
/// </summary>
/// <remarks>
/// The secret is the OpenIddict client secret the Web already shares with the Api (B-4 decision): no new
/// secret to provision. Behind a proxy the connection's address is the proxy's until forwarded headers
/// are configured, a deployment concern (<c>docs/infra.md</c>).
/// </remarks>
public sealed class ClientAddress(IConfiguration configuration)
{
    private readonly byte[] _trustedSecret = Encoding.UTF8.GetBytes(configuration["Authentication:OpenIddict:ClientSecret"] ?? string.Empty);

    /// <summary>The client's address as text, or null when the connection has none (in-process tests).</summary>
    public string? Of(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Request.Headers;
        if (IsFromTheWeb(headers[ClientAddressHeaders.Secret].ToString())
            && IPAddress.TryParse(headers[ClientAddressHeaders.Address].ToString(), out var visitor))
        {
            return visitor.ToString();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>The key of one client's bucket for one limit.</summary>
    public string KeyFor(HttpContext context, string scope) => $"{scope}:{Of(context) ?? "unknown"}";

    private bool IsFromTheWeb(string presented) =>
        _trustedSecret.Length > 0
        && presented.Length > 0
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), _trustedSecret);
}
