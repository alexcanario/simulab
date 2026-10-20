using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using Simulab.Identity.Infrastructure;
using Simulab.Testing;

namespace Simulab.Api.Tests;

/// <summary>
/// F-84 AC5, AC7, AC10, AC11, AC16, AC17 (BR4, BR5, BR9, BR11, T3, T4, T5): what the Api does at start with the previous
/// certificates. A fault in a previous certificate or in the listing never stops the start; what is loaded is logged by
/// thumbprint and nothing logged holds a certificate value.
/// </summary>
public class OpenIddictPreviousCertificateStartTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string SigningName = FakeCertificateVersionSource.SigningName;
    private const string EncryptionName = FakeCertificateVersionSource.EncryptionName;

    private static readonly DateTimeOffset Soon = DateTimeOffset.UtcNow.AddYears(1);

    private static readonly DateTimeOffset Later = DateTimeOffset.UtcNow.AddYears(2);

    private sealed record Started(IReadOnlyList<RecordedLogEntry> Log, OpenIddictServerOptions Options);

    private Started Start(
        string environment,
        TestCertificates? current,
        FakeCertificateVersionSource? source,
        TimeSpan? timeout = null,
        Action<IWebHostBuilder>? extra = null)
    {
        var recorder = new RecordingLoggerProvider();
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            CloudSettings.ApplyEmail(builder);
            if (current is not null)
            {
                builder.UseSetting("OpenIddict:SigningCertificate", current.Signing);
                builder.UseSetting("OpenIddict:EncryptionCertificate", current.Encryption);
            }

            builder.ConfigureLogging(logging => logging.AddProvider(recorder));
            builder.ConfigureServices(services =>
            {
                if (source is not null)
                {
                    services.AddSingleton<ICertificateVersionSource>(source);
                }

                services.Configure<PreviousCertificateOptions>(options => options.ListTimeout = timeout ?? TimeSpan.FromSeconds(10));
            });
            extra?.Invoke(builder);
        });

        _ = host.Services;
        var options = environment == "Development"
            ? new OpenIddictServerOptions()
            : host.Services.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;
        return new Started(recorder.Entries, options);
    }

    private static void ShouldHoldNoValue(Started started, params string[] values)
    {
        foreach (var value in values)
        {
            foreach (var entry in started.Log)
            {
                entry.Message.Should().NotContain(value[100..140]).And.NotContain(value[^40..], "no log line holds a part of a certificate value (AC10)");
            }
        }
    }

    private static IEnumerable<RecordedLogEntry> Warnings(Started started) =>
        started.Log.Where(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("OpenIddict", StringComparison.Ordinal));

    /// <summary>AC5 (BR4): a previous certificate that cannot do its job is ignored with a warning that names the certificate and the reason.</summary>
    [Theory]
    [InlineData(SigningName, "NotPkcs12", "PKCS#12")]
    [InlineData(SigningName, "NoPrivateKey", "no private key")]
    [InlineData(SigningName, "WrongUsage", "key usage")]
    [InlineData(SigningName, "NoUsage", "key usage")]
    [InlineData(SigningName, "Expired", "expired")]
    [InlineData(SigningName, "NotYetValid", "not valid yet")]
    [InlineData(SigningName, "NotEarlier", "earlier")]
    [InlineData(EncryptionName, "NotPkcs12", "PKCS#12")]
    [InlineData(EncryptionName, "NoPrivateKey", "no private key")]
    [InlineData(EncryptionName, "WrongUsage", "key usage")]
    [InlineData(EncryptionName, "NoUsage", "key usage")]
    [InlineData(EncryptionName, "Expired", "expired")]
    [InlineData(EncryptionName, "NotYetValid", "not valid yet")]
    [InlineData(EncryptionName, "NotEarlier", "earlier")]
    public void Start_PreviousCertificateCannotDoItsJob_StartsWithTheCurrentOneAndWarns(string secretName, string flaw, string reason)
    {
        var current = TestCertificates.Create(Soon);
        var good = TestCertificates.Create(Soon.AddDays(-30));
        var bad = BadCertificate(secretName, flaw, Soon);
        var source = FakeCertificateVersionSource.Of(current, previous: good)
            .With(secretName, FakeCertificateVersionSource.Enabled("v2", 2, secretName == SigningName ? current.Signing : current.Encryption), FakeCertificateVersionSource.Enabled("v1", 1, bad));

        var started = Start("Staging", current, source);

        started.Options.SigningCredentials.Should().HaveCount(secretName == SigningName ? 1 : 2);
        started.Options.EncryptionCredentials.Should().HaveCount(secretName == EncryptionName ? 1 : 2);
        Warnings(started).Should().ContainSingle(entry => entry.Message.Contains(secretName, StringComparison.Ordinal) && entry.Message.Contains(reason, StringComparison.OrdinalIgnoreCase));
        ShouldHoldNoValue(started, bad, current.Signing, current.Encryption);
    }

    private static string BadCertificate(string secretName, string flaw, DateTimeOffset currentNotAfter)
    {
        var other = secretName == SigningName ? X509KeyUsageFlags.KeyEncipherment : X509KeyUsageFlags.DigitalSignature;
        return flaw switch
        {
            "NotPkcs12" => "not-a-certificate" + new string('A', 260),
            "NoPrivateKey" => TestCertificates.CreateOne("flawed", withPrivateKey: false),
            "WrongUsage" => TestCertificates.CreateOne("flawed", usage: other),
            "NoUsage" => TestCertificates.CreateOne("flawed", usage: null),
            "Expired" => TestCertificates.CreateOne("flawed", notBefore: DateTimeOffset.UtcNow.AddYears(-2), notAfter: DateTimeOffset.UtcNow.AddDays(-1)),
            "NotYetValid" => TestCertificates.CreateOne("flawed", notBefore: DateTimeOffset.UtcNow.AddDays(1), notAfter: currentNotAfter.AddDays(-10)),
            "NotEarlier" => TestCertificates.CreateOne("flawed", notAfter: currentNotAfter.AddDays(30)),
            _ => throw new ArgumentOutOfRangeException(nameof(flaw)),
        };
    }

    /// <summary>AC7 (BR5, T4): when the listing fails, hangs or disagrees the host still starts with the current certificates and warns once per certificate.</summary>
    [Theory]
    [InlineData("Throws", "InvalidOperationException")]
    [InlineData("NeverAnswers", "timed out")]
    [InlineData("Disagrees", "current")]
    public void Start_ListingFails_StartsWithTheCurrentCertificatesAndWarnsOncePerCertificate(string failure, string reason)
    {
        var current = TestCertificates.Create(Soon);
        var previous = TestCertificates.Create(Soon.AddDays(-30));
        var source = FakeCertificateVersionSource.Of(current, previous);
        switch (failure)
        {
            case "Throws":
                source.Behavior = () => throw new InvalidOperationException("the vault said no, and this text could hold anything");
                break;
            case "NeverAnswers":
                source.Behavior = () => new TaskCompletionSource<IReadOnlyList<CertificateVersion>>().Task;
                break;
            default:
                source.With(SigningName, FakeCertificateVersionSource.Enabled("v3", 3, previous.Signing), FakeCertificateVersionSource.Enabled("v2", 2, current.Signing))
                    .With(EncryptionName, FakeCertificateVersionSource.Enabled("v3", 3, previous.Encryption), FakeCertificateVersionSource.Enabled("v2", 2, current.Encryption));
                break;
        }

        var started = Start("Staging", current, source, timeout: TimeSpan.FromMilliseconds(500));

        started.Options.SigningCredentials.Should().ContainSingle();
        started.Options.EncryptionCredentials.Should().ContainSingle();
        var warnings = Warnings(started).ToList();
        warnings.Should().HaveCount(2);
        warnings.Should().ContainSingle(entry => entry.Message.Contains(SigningName, StringComparison.Ordinal) && entry.Message.Contains(reason, StringComparison.OrdinalIgnoreCase));
        warnings.Should().ContainSingle(entry => entry.Message.Contains(EncryptionName, StringComparison.Ordinal) && entry.Message.Contains(reason, StringComparison.OrdinalIgnoreCase));
        started.Log.Select(entry => entry.Message).Should().NotContain(message => message.Contains("this text could hold anything", StringComparison.Ordinal), "only the type of an exception is logged");
        ShouldHoldNoValue(started, current.Signing, current.Encryption, previous.Signing, previous.Encryption);
    }

    /// <summary>AC9 (start side, BR6): a disabled previous version is not loaded and an older enabled one is not promoted in its place.</summary>
    [Fact]
    public void Start_PreviousVersionDisabled_LoadsNoPreviousCertificate()
    {
        var current = TestCertificates.Create(Soon);
        var older = TestCertificates.Create(Soon.AddDays(-30));
        var source = FakeCertificateVersionSource.Of(current)
            .With(SigningName, FakeCertificateVersionSource.Enabled("v3", 3, current.Signing), FakeCertificateVersionSource.Disabled("v2", 2), FakeCertificateVersionSource.Enabled("v1", 1, older.Signing))
            .With(EncryptionName, FakeCertificateVersionSource.Enabled("v3", 3, current.Encryption), FakeCertificateVersionSource.Disabled("v2", 2), FakeCertificateVersionSource.Enabled("v1", 1, older.Encryption));

        var started = Start("Staging", current, source);

        started.Options.SigningCredentials.Should().ContainSingle();
        started.Options.EncryptionCredentials.Should().ContainSingle();
        Warnings(started).Should().BeEmpty("a disabled previous version is the cut-off the operator asked for");
        ShouldHoldNoValue(started, current.Signing, older.Signing, older.Encryption);
    }

    /// <summary>AC3 (start side): one version of each certificate means no previous certificate and the start is silent about it.</summary>
    [Fact]
    public void Start_OneVersionOnly_LoadsNoPreviousCertificate()
    {
        var current = TestCertificates.Create(Soon);

        var started = Start("Staging", current, FakeCertificateVersionSource.Of(current));

        started.Options.SigningCredentials.Should().ContainSingle();
        started.Options.EncryptionCredentials.Should().ContainSingle();
        Warnings(started).Should().BeEmpty();
    }

    /// <summary>AC11 (BR9): in Development no version source is asked, and with no vault connection (no source) a Staging host starts as before.</summary>
    [Fact]
    public void Start_DevelopmentWithASourceThatMustNotBeCalled_NeverCallsIt()
    {
        var source = new FakeCertificateVersionSource { Behavior = () => throw new InvalidOperationException("must not be called") };

        _ = Start("Development", current: null, source);

        source.Calls.Should().Be(0);
    }

    [Fact]
    public void Start_NoVaultConnection_HasNoPreviousCertificate()
    {
        var current = TestCertificates.Create();

        var started = Start("Staging", current, source: null);

        started.Options.SigningCredentials.Should().ContainSingle();
        started.Options.EncryptionCredentials.Should().ContainSingle();
        Warnings(started).Should().BeEmpty();
    }

    /// <summary>AC16 (T5): a previous certificate offered through configuration is not read by the Api.</summary>
    [Fact]
    public void Start_PreviousCertificateOfferedThroughConfiguration_IsNotLoaded()
    {
        var current = TestCertificates.Create(Soon);
        var offered = TestCertificates.Create(Soon.AddDays(-30));

        var started = Start("Staging", current, source: null, extra: builder =>
        {
            builder.UseSetting("OpenIddict:PreviousSigningCertificate", offered.Signing);
            builder.UseSetting("OpenIddict:PreviousEncryptionCertificate", offered.Encryption);
        });

        started.Options.SigningCredentials.Should().ContainSingle();
        started.Options.EncryptionCredentials.Should().ContainSingle();
    }

    /// <summary>AC17 (T6, BR11): one Information line per role says which thumbprints are trusted and which is current or previous; none holds a value.</summary>
    [Fact]
    public void Start_WithAPreviousCertificate_LogsTheThumbprintsOfEachRole()
    {
        var current = TestCertificates.Create(Later);
        var previous = TestCertificates.Create(Soon);

        var started = Start("Staging", current, FakeCertificateVersionSource.Of(current, previous));

        started.Options.SigningCredentials.Should().HaveCount(2);
        started.Options.EncryptionCredentials.Should().HaveCount(2);
        var lines = started.Log.Where(entry => entry.Level == LogLevel.Information && entry.Message.Contains("OpenIddict", StringComparison.Ordinal) && entry.Message.Contains("previous", StringComparison.OrdinalIgnoreCase)).ToList();
        lines.Should().HaveCount(2, "one line per role");
        AssertLine(lines, "signing", current.Signing, previous.Signing);
        AssertLine(lines, "encryption", current.Encryption, previous.Encryption);
        ShouldHoldNoValue(started, current.Signing, current.Encryption, previous.Signing, previous.Encryption);
    }

    private static void AssertLine(List<RecordedLogEntry> lines, string role, string current, string previous)
    {
        var line = lines.Should().ContainSingle(entry => entry.Message.Contains(role, StringComparison.OrdinalIgnoreCase)).Which.Message;
        line.Should().Contain(TestCertificates.Thumbprint(current)).And.Contain(TestCertificates.Thumbprint(previous)).And.Contain("current");
    }
}
