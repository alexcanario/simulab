using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using Simulab.Testing;

namespace Simulab.Api.Tests;

/// <summary>
/// F-64 AC2 (start side): outside Development the Api loads its OpenIddict certificates from configuration and refuses to
/// start, naming the setting, when one is missing or is not a PKCS#12 file. The token surviving a restart is
/// <c>Simulab.Identity.Tests</c> (it needs a database).
/// </summary>
public class OpenIddictCertificateStartTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private Action Start(string environment, Action<IWebHostBuilder> configure)
    {
        var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudSettings.ApplyEmail(builder);
            configure(builder);
        });
        return () =>
        {
            using (host)
            {
                _ = host.Services;
            }
        };
    }

    [Theory]
    [InlineData("Staging", "OpenIddict:SigningCertificate")]
    [InlineData("Production", "OpenIddict:SigningCertificate")]
    public void Start_NoCertificates_RefusesAndNamesTheSetting(string environment, string key)
    {
        var start = Start(environment, _ => { });

        start.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    [Fact]
    public void Start_OnlyTheSigningCertificate_RefusesAndNamesTheEncryptionOne()
    {
        var certificates = TestCertificates.Create();

        var start = Start("Staging", builder => builder.UseSetting("OpenIddict:SigningCertificate", certificates.Signing));

        start.Should().Throw<InvalidOperationException>().WithMessage("*OpenIddict:EncryptionCertificate*");
    }

    [Fact]
    public void Start_CertificateIsNotAPkcs12File_RefusesAndNamesTheSetting()
    {
        var certificates = TestCertificates.Create();

        var start = Start("Staging", builder =>
        {
            builder.UseSetting("OpenIddict:SigningCertificate", "not-a-certificate");
            builder.UseSetting("OpenIddict:EncryptionCertificate", certificates.Encryption);
        });

        start.Should().Throw<InvalidOperationException>().WithMessage("*OpenIddict:SigningCertificate*PKCS#12*");
    }

    /// <summary>The rule of presence: with both certificates the host starts and OpenIddict holds them.</summary>
    [Fact]
    public void Start_BothCertificates_OpenIddictUsesThem()
    {
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Staging");
            CloudSettings.Apply(builder);
        });

        var options = host.Services.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;

        options.SigningCredentials.Should().ContainSingle();
        options.EncryptionCredentials.Should().ContainSingle();
    }
}
