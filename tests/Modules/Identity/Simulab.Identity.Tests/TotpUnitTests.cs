using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OtpNet;
using Simulab.Identity.Application.Totp;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Totp;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-11 without I/O: the secret protector and the TOTP arithmetic (imported from Simulae's
/// <c>AesGcmTotpSecretProtectorTests</c> and <c>TotpServiceTests</c>), the key check and the replay rule.
/// </summary>
public sealed class TotpUnitTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static AesGcmTotpSecretProtector Protector(string? key = null) =>
        new(Options.Create(new TotpOptions { TotpEnabled = true, TotpEncryptionKey = key ?? TotpApi.TestKey }));

    [Fact]
    public void Protector_ProtectThenUnprotect_ReturnsTheSecretAndStoresSomethingElse()
    {
        var protector = Protector();
        const string secret = "JBSWY3DPEHPK3PXP";

        var stored = protector.Protect(secret);

        stored.Should().NotContain(secret);
        protector.Unprotect(stored).Should().Be(secret);
        protector.Protect(secret).Should().NotBe(stored, "every value gets its own nonce");
    }

    [Fact]
    public void Protector_TamperedValue_Throws()
    {
        var protector = Protector();
        var bytes = Convert.FromBase64String(protector.Protect("JBSWY3DPEHPK3PXP"));
        bytes[15] ^= 0xFF;

        var unprotect = () => protector.Unprotect(Convert.ToBase64String(bytes));

        unprotect.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Protector_ValueWrittenWithAnotherKey_Throws()
    {
        var stored = Protector(Convert.ToBase64String(new byte[32])).Protect("JBSWY3DPEHPK3PXP");

        var unprotect = () => Protector().Unprotect(stored);

        unprotect.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Protector_Hash_DependsOnTheKey()
    {
        Protector().Hash("ABCDEFGHJK").Should().HaveLength(64)
            .And.NotBe(Protector(Convert.ToBase64String(new byte[32])).Hash("ABCDEFGHJK"));
    }

    [Fact]
    public void Authenticator_GenerateSecret_IsTwentyBytesOfBase32()
    {
        var secret = new TotpAuthenticator().GenerateSecret();

        secret.Should().HaveLength(32);
        Base32Encoding.ToBytes(secret).Should().HaveCount(20);
    }

    [Fact]
    public void Authenticator_OtpAuthUri_CarriesIssuerLabelAndParameters()
    {
        var uri = new TotpAuthenticator().OtpAuthUri("aluno+teste@exemplo.com", "JBSWY3DPEHPK3PXP");

        uri.Should().Be("otpauth://totp/Simulab%3Aaluno%2Bteste%40exemplo.com?secret=JBSWY3DPEHPK3PXP&issuer=Simulab&digits=6&period=30");
    }

    [Fact]
    public void Authenticator_MatchStep_AcceptsOneStepEitherSideAndReturnsWhichStep()
    {
        var authenticator = new TotpAuthenticator();
        var secret = authenticator.GenerateSecret();
        var currentStep = At.ToUnixTimeSeconds() / 30;

        authenticator.MatchStep(secret, TotpApi.CodeAt(secret, At), At).Should().Be(currentStep);
        authenticator.MatchStep(secret, TotpApi.CodeAt(secret, At.AddSeconds(-30)), At).Should().Be(currentStep - 1);
        authenticator.MatchStep(secret, TotpApi.CodeAt(secret, At.AddSeconds(30)), At).Should().Be(currentStep + 1);
    }

    [Fact]
    public void Authenticator_MatchStep_RefusesCodesOutsideTheWindow()
    {
        var authenticator = new TotpAuthenticator();
        var secret = authenticator.GenerateSecret();

        authenticator.MatchStep(secret, TotpApi.CodeAt(secret, At.AddSeconds(-90)), At).Should().BeNull();
        authenticator.MatchStep(secret, TotpApi.CodeAt(secret, At.AddSeconds(90)), At).Should().BeNull();
    }

    [Fact]
    public void Authenticator_QrCodeDataUri_IsAPng()
    {
        var png = new TotpAuthenticator().QrCodeDataUri("otpauth://totp/Simulab%3Aa%40b.com?secret=JBSWY3DPEHPK3PXP");

        png.Should().StartWith("data:image/png;base64,");
        Convert.FromBase64String(png["data:image/png;base64,".Length..])[..4].Should().Equal(0x89, 0x50, 0x4E, 0x47);
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(true, null, false)]
    [InlineData(true, "", false)]
    [InlineData(true, "bm90LWEtMjU2LWJpdC1rZXk=", false)]
    [InlineData(true, "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=", true)]
    public void Options_Problem_OnlyWhenOnWithoutA256BitKey(bool enabled, string? key, bool valid)
    {
        var problem = new TotpOptions { TotpEnabled = enabled, TotpEncryptionKey = key }.Problem();

        if (valid)
        {
            problem.Should().BeNull();
        }
        else
        {
            problem.Should().Contain("Identity:TotpEncryptionKey");
        }
    }

    [Fact]
    public void User_AcceptTotpStep_RefusesTheSameOrAnEarlierStep()
    {
        var user = new User();

        user.AcceptTotpStep(100).Should().BeTrue();
        user.AcceptTotpStep(100).Should().BeFalse();
        user.AcceptTotpStep(99).Should().BeFalse();
        user.AcceptTotpStep(101).Should().BeTrue();
    }

    [Fact]
    public void User_StartTotpEnrolment_ForgetsTheLastStep()
    {
        var user = new User();
        user.AcceptTotpStep(100);

        user.StartTotpEnrolment("protected");

        user.TotpLastAcceptedStep.Should().BeNull();
        user.TwoFactorEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData("abcde-fghjk", "ABCDEFGHJK")]
    [InlineData(" ABCDE FGHJK ", "ABCDEFGHJK")]
    [InlineData(null, "")]
    public void RecoveryCodes_Normalize_IgnoresCaseDashAndSpaces(string? input, string expected) =>
        RecoveryCodes.Normalize(input).Should().Be(expected);
}
