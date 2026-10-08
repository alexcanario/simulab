using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Simulab.Testing;
using Simulab.Testing.ApiHost;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-64 AC2 (restart side, BR4): outside Development a token issued before a restart is still accepted afterwards, because
/// the host signs and encrypts with the configured certificates and not with ones it made for itself. A host with other
/// certificates does not accept it, which is what the control proves: the test can fail.
/// </summary>
public sealed class OpenIddictCertificateRestartTests
{
    private static SimulabApiFactory StagingHost(TestCertificates certificates) => new()
    {
        ConfigureHost = builder =>
        {
            builder.UseEnvironment("Staging");
            builder.UseSetting("OpenIddict:SigningCertificate", certificates.Signing);
            builder.UseSetting("OpenIddict:EncryptionCertificate", certificates.Encryption);
            builder.UseSetting("Email:Provider", "AzureCommunicationServices");
            builder.UseSetting("Email:AzureCommunicationServices:Endpoint", "https://simulab-test.communication.azure.com");
            builder.UseSetting("Email:FromAddress", "DoNotReply@test.azurecomm.net");
            builder.UseSetting("AZURE_CLIENT_ID", "00000000-0000-0000-0000-000000000001");
        },
    };

    /// <summary>TLS ends at the ingress in the cloud; the test server is given the https scheme directly.</summary>
    private static HttpClient Https(SimulabApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private static async Task<HttpStatusCode> ProfileStatusAsync(SimulabApiFactory factory, string accessToken)
    {
        using var client = Https(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.GetAsync("/api/v1/identity/profile");
        return response.StatusCode;
    }

    [Fact]
    public async Task Restart_SameCertificates_TheTokenIssuedBeforeIsStillAccepted()
    {
        var certificates = TestCertificates.Create();
        string connectionString;
        string accessToken;

        await using (var first = StagingHost(certificates))
        {
            await first.PrepareAsync(nameof(OpenIddictCertificateRestartTests));
            var user = await TestAccounts.CreateAsync(first.Services);
            using var client = Https(first);
            var token = await TokenClient.SignInAsync(client, user.Email!, TestAccounts.ValidPassword);
            token.AccessToken.Should().NotBeNull($"{token.Error}: {token.ErrorDescription}");
            accessToken = token.AccessToken!;
            (await ProfileStatusAsync(first, accessToken)).Should().Be(HttpStatusCode.OK, "the host that issued it accepts it");
            connectionString = first.ConnectionString;
        }

        await using var second = StagingHost(certificates);
        await second.PrepareExistingAsync(connectionString);

        (await ProfileStatusAsync(second, accessToken)).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Restart_OtherCertificates_TheTokenIssuedBeforeIsRefused()
    {
        string connectionString;
        string accessToken;

        await using (var first = StagingHost(TestCertificates.Create()))
        {
            await first.PrepareAsync(nameof(Restart_OtherCertificates_TheTokenIssuedBeforeIsRefused));
            var user = await TestAccounts.CreateAsync(first.Services);
            using var client = Https(first);
            var token = await TokenClient.SignInAsync(client, user.Email!, TestAccounts.ValidPassword);
            accessToken = token.AccessToken!;
            (await ProfileStatusAsync(first, accessToken)).Should().Be(HttpStatusCode.OK, "the control only means something if the token was good");
            connectionString = first.ConnectionString;
        }

        await using var second = StagingHost(TestCertificates.Create());
        await second.PrepareExistingAsync(connectionString);

        (await ProfileStatusAsync(second, accessToken)).Should().Be(HttpStatusCode.Unauthorized);
    }
}
