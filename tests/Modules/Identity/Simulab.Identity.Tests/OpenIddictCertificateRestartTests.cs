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

    /// <summary>F-73 AC2 (UC2): what keeps a student signed in across a deploy is the refresh token, not the 15-minute access token.</summary>
    [Fact]
    public async Task Restart_SameCertificates_TheRefreshTokenIssuedBeforeIsRedeemed()
    {
        var certificates = TestCertificates.Create();
        string connectionString;
        string refreshToken;

        await using (var first = StagingHost(certificates))
        {
            await first.PrepareAsync(nameof(Restart_SameCertificates_TheRefreshTokenIssuedBeforeIsRedeemed));
            var user = await TestAccounts.CreateAsync(first.Services);
            using var client = Https(first);
            var token = await TokenClient.SignInAsync(client, user.Email!, TestAccounts.ValidPassword);
            refreshToken = token.RefreshToken!;
            connectionString = first.ConnectionString;
        }

        await using var second = StagingHost(certificates);
        await second.PrepareExistingAsync(connectionString);
        using var secondClient = Https(second);

        var renewed = await TokenClient.RefreshAsync(secondClient, refreshToken);

        renewed.AccessToken.Should().NotBeNull($"{renewed.Error}: {renewed.ErrorDescription}");
    }

    /// <summary>
    /// F-73 AC1 (UC1, BR1): two replicas running at the same time, on the same database, with the same certificates: the
    /// second accepts what the first issued, and redeems its refresh token.
    /// </summary>
    [Fact]
    public async Task TwoReplicas_SameCertificates_TheSecondAcceptsTheAccessTokenAndRedeemsTheRefreshToken()
    {
        var certificates = TestCertificates.Create();

        await using var first = StagingHost(certificates);
        await first.PrepareAsync(nameof(TwoReplicas_SameCertificates_TheSecondAcceptsTheAccessTokenAndRedeemsTheRefreshToken));
        await using var second = StagingHost(certificates);
        await second.PrepareExistingAsync(first.ConnectionString);

        var user = await TestAccounts.CreateAsync(first.Services);
        using var firstClient = Https(first);
        var token = await TokenClient.SignInAsync(firstClient, user.Email!, TestAccounts.ValidPassword);
        token.AccessToken.Should().NotBeNull($"{token.Error}: {token.ErrorDescription}");

        (await ProfileStatusAsync(second, token.AccessToken!)).Should().Be(HttpStatusCode.OK);

        using var secondClient = Https(second);
        var renewed = await TokenClient.RefreshAsync(secondClient, token.RefreshToken!);
        renewed.AccessToken.Should().NotBeNull($"{renewed.Error}: {renewed.ErrorDescription}");
    }

    /// <summary>
    /// F-73 AC3 (BR1): the control of the test above. Two replicas that differ only in the signing certificate: the second
    /// refuses the first one's access token, so the test above cannot pass by accident.
    /// </summary>
    [Fact]
    public async Task TwoReplicas_OtherSigningCertificate_TheSecondRefusesTheAccessToken()
    {
        var certificates = TestCertificates.Create();
        var otherSigning = certificates with { Signing = TestCertificates.Create().Signing };

        await using var first = StagingHost(certificates);
        await first.PrepareAsync(nameof(TwoReplicas_OtherSigningCertificate_TheSecondRefusesTheAccessToken));
        await using var second = StagingHost(otherSigning);
        await second.PrepareExistingAsync(first.ConnectionString);

        var user = await TestAccounts.CreateAsync(first.Services);
        using var firstClient = Https(first);
        var token = await TokenClient.SignInAsync(firstClient, user.Email!, TestAccounts.ValidPassword);

        (await ProfileStatusAsync(first, token.AccessToken!)).Should().Be(HttpStatusCode.OK, "the control only means something if the token was good");
        (await ProfileStatusAsync(second, token.AccessToken!)).Should().Be(HttpStatusCode.Unauthorized);
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
