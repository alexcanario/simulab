using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Simulab.Identity.Api;

namespace Simulab.Identity.Tests;

/// <summary>F-38 BR8, AC10: how the client key groups addresses, for every client limit.</summary>
public class ClientAddressKeyTests
{
    private static string KeyOf(string address)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(address);
        return new ClientAddress(new ConfigurationBuilder().Build()).KeyFor(context, "scope");
    }

    [Fact]
    public void KeyFor_TwoAddressesOfTheSameIpv6Prefix_IsOneKey()
    {
        KeyOf("2001:db8::1").Should().Be(KeyOf("2001:db8::2"));
        KeyOf("2001:db8:0:1::1").Should().NotBe(KeyOf("2001:db8:0:2::1"), "another /64 is another client");
    }

    [Fact]
    public void KeyFor_AnIpv4AddressMappedIntoIpv6_IsTheIpv4Key()
    {
        KeyOf("::ffff:203.0.113.5").Should().Be(KeyOf("203.0.113.5"));
    }

    [Fact]
    public void KeyFor_DifferentIpv4Addresses_AreDifferentKeys()
    {
        KeyOf("203.0.113.5").Should().NotBe(KeyOf("203.0.113.6"));
    }

    [Fact]
    public void KeyFor_NoAddress_IsTheUnknownBucket()
    {
        new ClientAddress(new ConfigurationBuilder().Build()).KeyFor(new DefaultHttpContext(), "scope").Should().Be("scope:unknown");
    }
}
