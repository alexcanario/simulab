using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Api;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// B-4: the Web calls the Api from its own server, so the visitor's address travels in a header the Api
/// trusts only with the Web's secret. AC1-AC3 and AC6.
/// </summary>
public sealed class ClientAddressTests : IdentityApiTests
{
    private const string ResetRequestRoute = "/api/v1/identity/password-reset-requests";

    private static async Task<HttpResponseMessage> AskForResetAsync(HttpClient client, string? address, string? secret)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ResetRequestRoute)
        {
            Content = JsonContent.Create(new RequestPasswordResetRequest("ninguem@exemplo.com"), options: AppJson.Options)
        };
        if (address is not null)
        {
            request.Headers.Add(ClientAddressHeaders.Address, address);
        }

        if (secret is not null)
        {
            request.Headers.Add(ClientAddressHeaders.Secret, secret);
        }

        return await client.SendAsync(request);
    }

    /// <summary>AC1: two visitors behind the same Web server have a bucket each.</summary>
    [Fact]
    public async Task TwoVisitors_ThroughTheWeb_HaveTheirOwnLimit()
    {
        var web = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetRequestsPerHour; i++)
        {
            using var used = await AskForResetAsync(web, "203.0.113.10", TestClient.ClientSecret);
            used.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        using var sameVisitor = await AskForResetAsync(web, "203.0.113.10", TestClient.ClientSecret);
        using var otherVisitor = await AskForResetAsync(web, "198.51.100.20", TestClient.ClientSecret);

        sameVisitor.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        otherVisitor.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    /// <summary>AC2: without the Web's secret, or with something that is not an address, the header is ignored.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("not-the-secret")]
    public async Task AddressHeader_WithoutTheWebSecret_IsIgnored(string? secret)
    {
        var caller = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetRequestsPerHour; i++)
        {
            using var used = await AskForResetAsync(caller, $"203.0.113.{i + 1}", secret);
            used.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        using var claimingAnotherAddress = await AskForResetAsync(caller, "198.51.100.20", secret);

        claimingAnotherAddress.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AddressHeader_ThatIsNotAnAddress_IsIgnored()
    {
        var web = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetRequestsPerHour; i++)
        {
            using var used = await AskForResetAsync(web, $"visitor-{i}", TestClient.ClientSecret);
            used.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        using var another = await AskForResetAsync(web, "visitor-99", TestClient.ClientSecret);

        another.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>F-38 AC10, BR8: two addresses of one IPv6 /64, or an IPv4 address and its mapped form, are one client.</summary>
    [Theory]
    [InlineData("2001:db8::1", "2001:db8::2")]
    [InlineData("::ffff:203.0.113.5", "203.0.113.5")]
    public async Task ClientLimits_GroupAddressesOfTheSameClient(string first, string second)
    {
        var web = Client();
        for (var i = 0; i < IdentityRateLimits.PasswordResetRequestsPerHour; i++)
        {
            using var used = await AskForResetAsync(web, first, TestClient.ClientSecret);
            used.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        using var sameClient = await AskForResetAsync(web, second, TestClient.ClientSecret);

        sameClient.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>AC3: the consent record, the proof of acceptance, keeps the visitor's address.</summary>
    [Fact]
    public async Task SignUp_ThroughTheWeb_RecordsTheVisitorsAddressInTheConsent()
    {
        var form = SignUpForm.Valid("consentimento@exemplo.com");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/registrations")
        {
            Content = JsonContent.Create(form, options: AppJson.Options)
        };
        request.Headers.Add(ClientAddressHeaders.Address, "203.0.113.10");
        request.Headers.Add(ClientAddressHeaders.Secret, TestClient.ClientSecret);

        using var response = await Client().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var consent = await QueryAsync(context => context.ConsentRecords.SingleAsync());
        consent.IpAddress.Should().Be("203.0.113.10");
    }
}
