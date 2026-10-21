using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using Simulab.Identity.Infrastructure;
using Simulab.Testing;
using Simulab.Testing.ApiHost;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-84 AC1, AC2, AC8, AC9, AC16 (BR1, BR2, BR6, T1, T2, T5): across a renewal a token made with the previous pair is still
/// read, a token made after it is made with the new pair, and a pair that is neither current nor previous is refused.
/// </summary>
public sealed class OpenIddictPreviousCertificateTokenTests
{
    private static readonly DateTimeOffset Soon = DateTimeOffset.UtcNow.AddYears(1);

    private static readonly DateTimeOffset Later = DateTimeOffset.UtcNow.AddYears(2);

    private static SimulabApiFactory StagingHost(TestCertificates current, FakeCertificateVersionSource? source = null, Action<IWebHostBuilder>? extra = null) => new()
    {
        ConfigureHost = builder =>
        {
            builder.UseEnvironment("Staging");
            builder.UseSetting("OpenIddict:SigningCertificate", current.Signing);
            builder.UseSetting("OpenIddict:EncryptionCertificate", current.Encryption);
            builder.UseSetting("Email:Provider", "AzureCommunicationServices");
            builder.UseSetting("Email:AzureCommunicationServices:Endpoint", "https://simulab-test.communication.azure.com");
            builder.UseSetting("Email:FromAddress", "DoNotReply@test.azurecomm.net");
            builder.UseSetting("AZURE_CLIENT_ID", "00000000-0000-0000-0000-000000000001");
            if (source is not null)
            {
                builder.ConfigureServices(services => services.AddSingleton<ICertificateVersionSource>(source));
            }

            extra?.Invoke(builder);
        },
    };

    private static HttpClient Https(SimulabApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private static async Task<HttpStatusCode> ProfileStatusAsync(SimulabApiFactory factory, string accessToken)
    {
        using var client = Https(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.GetAsync("/api/v1/identity/profile");
        return response.StatusCode;
    }

    private static async Task<TokenResponse> SignInAsync(SimulabApiFactory factory)
    {
        var user = await TestAccounts.CreateAsync(factory.Services);
        using var client = Https(factory);
        var token = await TokenClient.SignInAsync(client, user.Email!, TestAccounts.ValidPassword);
        token.AccessToken.Should().NotBeNull($"{token.Error}: {token.ErrorDescription}");
        return token;
    }

    private static async Task<TokenResponse> RefreshAsync(SimulabApiFactory factory, string refreshToken)
    {
        using var client = Https(factory);
        return await TokenClient.RefreshAsync(client, refreshToken);
    }

    /// <summary>AC1 (BR1, BR2, UC1): the host that has the new pair as current and the old one as previous reads what the old host made.</summary>
    [Fact]
    public async Task Renewal_TokenMadeWithThePreviousPair_TheRenewedHostAcceptsItAndRedeemsTheRefreshToken()
    {
        var pairA = TestCertificates.Create(Soon);
        var pairB = TestCertificates.Create(Later);

        await using var first = StagingHost(pairA);
        await first.PrepareAsync(nameof(Renewal_TokenMadeWithThePreviousPair_TheRenewedHostAcceptsItAndRedeemsTheRefreshToken));
        await using var second = StagingHost(pairB, FakeCertificateVersionSource.Of(pairB, previous: pairA));
        await second.PrepareExistingAsync(first.ConnectionString);

        var token = await SignInAsync(first);

        (await ProfileStatusAsync(second, token.AccessToken!)).Should().Be(HttpStatusCode.OK);
        var renewed = await RefreshAsync(second, token.RefreshToken!);
        renewed.AccessToken.Should().NotBeNull($"{renewed.Error}: {renewed.ErrorDescription}");
    }

    /// <summary>AC2 (BR2, BR8): what the renewed host makes is made with the new pair, so a host that only has the old pair refuses it.</summary>
    [Fact]
    public async Task Renewal_TokenMadeAfterIt_IsMadeWithTheNewPairAndTheOverlapIsReal()
    {
        var pairA = TestCertificates.Create(Soon);
        var pairB = TestCertificates.Create(Later);

        await using var oldHost = StagingHost(pairA);
        await oldHost.PrepareAsync(nameof(Renewal_TokenMadeAfterIt_IsMadeWithTheNewPairAndTheOverlapIsReal));
        await using var renewed = StagingHost(pairB, FakeCertificateVersionSource.Of(pairB, previous: pairA));
        await renewed.PrepareExistingAsync(oldHost.ConnectionString);
        await using var newOnly = StagingHost(pairB);
        await newOnly.PrepareExistingAsync(oldHost.ConnectionString);

        var token = await SignInAsync(renewed);

        (await ProfileStatusAsync(newOnly, token.AccessToken!)).Should().Be(HttpStatusCode.OK);
        (await RefreshAsync(newOnly, token.RefreshToken!)).AccessToken.Should().NotBeNull();
        (await ProfileStatusAsync(oldHost, token.AccessToken!)).Should().Be(HttpStatusCode.Unauthorized, "a replica that still holds only the old pair refuses the new token (BR8)");
    }

    /// <summary>AC2, the last sentence: the order the certificates are registered in does not decide which one signs; OpenIddict puts the one that expires last first.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Options_PreviousRegisteredBeforeOrAfterTheCurrent_TheOneThatExpiresLastComesFirst(bool previousFirst)
    {
        var older = X509CertificateLoaderForTests.Load(TestCertificates.CreateOne("older", notAfter: Soon));
        var newer = X509CertificateLoaderForTests.Load(TestCertificates.CreateOne("newer", notAfter: Later));

        var services = new ServiceCollection();
        services.AddOpenIddict().AddServer(options =>
        {
            options.SetTokenEndpointUris("/connect/token");
            options.AllowPasswordFlow();
            options.AcceptAnonymousClients();
            options.UseAspNetCore().EnableTokenEndpointPassthrough();
            foreach (var certificate in previousFirst ? new[] { older, newer } : new[] { newer, older })
            {
                options.AddSigningCertificate(certificate).AddEncryptionCertificate(certificate);
            }
        });

        await using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;

        resolved.SigningCredentials[0].Key.KeyId.Should().Be(new Microsoft.IdentityModel.Tokens.X509SecurityKey(newer).KeyId);
        resolved.EncryptionCredentials[0].Key.KeyId.Should().Be(new Microsoft.IdentityModel.Tokens.X509SecurityKey(newer).KeyId);
    }

    /// <summary>AC8 (T1): a pair that is neither current nor previous is refused, access token and refresh token alike.</summary>
    [Fact]
    public async Task Renewal_TokenMadeWithAnUnknownPair_IsRefusedWith401AndInvalidGrant()
    {
        var pairA = TestCertificates.Create(Soon);
        var pairB = TestCertificates.Create(Later);
        var stranger = TestCertificates.Create();

        await using var strangerHost = StagingHost(stranger);
        await strangerHost.PrepareAsync(nameof(Renewal_TokenMadeWithAnUnknownPair_IsRefusedWith401AndInvalidGrant));
        await using var renewed = StagingHost(pairB, FakeCertificateVersionSource.Of(pairB, previous: pairA));
        await renewed.PrepareExistingAsync(strangerHost.ConnectionString);

        var token = await SignInAsync(strangerHost);

        (await ProfileStatusAsync(strangerHost, token.AccessToken!)).Should().Be(HttpStatusCode.OK, "the control only means something if the token was good");
        (await ProfileStatusAsync(renewed, token.AccessToken!)).Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(renewed, token.RefreshToken!)).Error.Should().Be("invalid_grant");
    }

    /// <summary>AC9 (T2, BR6): the operator disabled the previous version; neither it nor an older enabled one is trusted.</summary>
    [Fact]
    public async Task Renewal_PreviousVersionDisabled_NeitherItNorAnOlderOneIsTrusted()
    {
        var pairOld = TestCertificates.Create(Soon);
        var pairPrevious = TestCertificates.Create(Soon);
        var pairB = TestCertificates.Create(Later);
        var source = FakeCertificateVersionSource.Of(pairB)
            .With(FakeCertificateVersionSource.SigningName, FakeCertificateVersionSource.Enabled("v3", 3, pairB.Signing), FakeCertificateVersionSource.Disabled("v2", 2), FakeCertificateVersionSource.Enabled("v1", 1, pairOld.Signing))
            .With(FakeCertificateVersionSource.EncryptionName, FakeCertificateVersionSource.Enabled("v3", 3, pairB.Encryption), FakeCertificateVersionSource.Disabled("v2", 2), FakeCertificateVersionSource.Enabled("v1", 1, pairOld.Encryption));

        await using var previousHost = StagingHost(pairPrevious);
        await previousHost.PrepareAsync(nameof(Renewal_PreviousVersionDisabled_NeitherItNorAnOlderOneIsTrusted));
        await using var oldHost = StagingHost(pairOld);
        await oldHost.PrepareExistingAsync(previousHost.ConnectionString);
        await using var renewed = StagingHost(pairB, source);
        await renewed.PrepareExistingAsync(previousHost.ConnectionString);

        var madeByPrevious = await SignInAsync(previousHost);
        var madeByOld = await SignInAsync(oldHost);

        (await ProfileStatusAsync(renewed, madeByPrevious.AccessToken!)).Should().Be(HttpStatusCode.Unauthorized);
        (await ProfileStatusAsync(renewed, madeByOld.AccessToken!)).Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>AC16 (T5, BR1): a previous certificate set through the environment or a vault secret under another name has no effect.</summary>
    [Fact]
    public async Task Renewal_PreviousPairSetAsConfiguration_IsNotTrusted()
    {
        var pairA = TestCertificates.Create(Soon);
        var pairB = TestCertificates.Create(Later);

        await using var oldHost = StagingHost(pairA);
        await oldHost.PrepareAsync(nameof(Renewal_PreviousPairSetAsConfiguration_IsNotTrusted));
        await using var other = StagingHost(pairB, extra: builder =>
        {
            builder.UseSetting("OpenIddict:PreviousSigningCertificate", pairA.Signing);
            builder.UseSetting("OpenIddict:PreviousEncryptionCertificate", pairA.Encryption);
        });
        await other.PrepareExistingAsync(oldHost.ConnectionString);

        var token = await SignInAsync(oldHost);

        (await ProfileStatusAsync(oldHost, token.AccessToken!)).Should().Be(HttpStatusCode.OK, "the control only means something if the token was good");
        (await ProfileStatusAsync(other, token.AccessToken!)).Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(other, token.RefreshToken!)).Error.Should().Be("invalid_grant");
    }
}

/// <summary>Loads a base64 PKCS#12 test certificate into an object OpenIddict can take.</summary>
internal static class X509CertificateLoaderForTests
{
    internal static System.Security.Cryptography.X509Certificates.X509Certificate2 Load(string base64) =>
        System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(base64), password: null);
}
