using Simulab.Identity.Application.Security;

namespace Simulab.Identity.Tests;

/// <summary>BR8: what goes in the email and what goes in the database are not the same value.</summary>
public class SecureTokenTests
{
    [Fact]
    public void Generate_ReturnsARawTokenAndItsHash()
    {
        var (raw, hash) = SecureToken.Generate();

        raw.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(raw);
        hash.Should().Be(SecureToken.Hash(raw));
        hash.Length.Should().Be(64, "SHA-256 in lower-case hexadecimal");
    }

    [Fact]
    public void Generate_NeverRepeatsATokenOrAHash()
    {
        var tokens = Enumerable.Range(0, 200).Select(_ => SecureToken.Generate()).ToList();

        tokens.Select(token => token.RawToken).Should().OnlyHaveUniqueItems();
        tokens.Select(token => token.TokenHash).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void RawToken_IsSafeInAUrl()
    {
        var (raw, _) = SecureToken.Generate();

        raw.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }
}
