using System.Security.Cryptography.X509Certificates;
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

    /// <summary>
    /// F-73 AC5 (BR3): a certificate that loads but could not do its job stops the start too, one case per reason and
    /// per key, and the message names the setting and the reason without any part of the value.
    /// </summary>
    [Theory]
    [InlineData("OpenIddict:SigningCertificate", "NoPrivateKey", "no private key")]
    [InlineData("OpenIddict:SigningCertificate", "WrongUsage", "digital signature")]
    [InlineData("OpenIddict:SigningCertificate", "NoUsage", "digital signature")]
    [InlineData("OpenIddict:SigningCertificate", "Expired", "expired")]
    [InlineData("OpenIddict:SigningCertificate", "NotYetValid", "not valid yet")]
    [InlineData("OpenIddict:EncryptionCertificate", "NoPrivateKey", "no private key")]
    [InlineData("OpenIddict:EncryptionCertificate", "WrongUsage", "key encipherment")]
    [InlineData("OpenIddict:EncryptionCertificate", "NoUsage", "key encipherment")]
    [InlineData("OpenIddict:EncryptionCertificate", "Expired", "expired")]
    [InlineData("OpenIddict:EncryptionCertificate", "NotYetValid", "not valid yet")]
    public void Start_CertificateCannotDoItsJob_RefusesNamingTheSettingAndTheReason(string key, string flaw, string reason)
    {
        var good = TestCertificates.Create();
        var bad = BadCertificate(key, flaw);

        var start = Start("Staging", builder =>
        {
            builder.UseSetting("OpenIddict:SigningCertificate", key == "OpenIddict:SigningCertificate" ? bad : good.Signing);
            builder.UseSetting("OpenIddict:EncryptionCertificate", key == "OpenIddict:EncryptionCertificate" ? bad : good.Encryption);
        });

        var message = start.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*{reason}*").Which.Message;
        message.Should().NotContain(bad[..40]);
    }

    private static string BadCertificate(string key, string flaw)
    {
        // The usage the role needs is the one a "wrong usage" certificate lacks: signing needs digital signature,
        // encryption needs key encipherment.
        var other = key == "OpenIddict:SigningCertificate" ? X509KeyUsageFlags.KeyEncipherment : X509KeyUsageFlags.DigitalSignature;
        return flaw switch
        {
            "NoPrivateKey" => TestCertificates.CreateOne("flawed", withPrivateKey: false),
            "WrongUsage" => TestCertificates.CreateOne("flawed", usage: other),
            "NoUsage" => TestCertificates.CreateOne("flawed", usage: null),
            "Expired" => TestCertificates.CreateOne("flawed", notBefore: DateTimeOffset.UtcNow.AddYears(-2), notAfter: DateTimeOffset.UtcNow.AddDays(-1)),
            "NotYetValid" => TestCertificates.CreateOne("flawed", notBefore: DateTimeOffset.UtcNow.AddDays(1), notAfter: DateTimeOffset.UtcNow.AddYears(2)),
            _ => throw new ArgumentOutOfRangeException(nameof(flaw)),
        };
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
